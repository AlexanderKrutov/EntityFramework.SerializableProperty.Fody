using System;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using EntityFramework.SerializableProperty;

public class ModuleWeaver
{
    private TypeSystem mTypeSystem;
    private MethodReference mCompilerGeneratedAttributeReference;
    private TypeDefinition mISerializerTypeDefinition;

    /// <summary>
    /// Will log an informational message to MSBuild
    /// </summary>
    public Action<string> LogInfo { get; set; }

    /// <summary>
    /// Will log an error message to MSBuild
    /// </summary>
    public Action<string> LogError { get; set; }

    /// <summary>
    /// An instance of Mono.Cecil.ModuleDefinition for processing
    /// </summary>
    public ModuleDefinition ModuleDefinition { get; set; }

    /// <summary>
    /// Init logging delegates to make testing easier.
    /// </summary>
    public ModuleWeaver()
    {
        LogInfo = m => { Debug.WriteLine("Info: " + m); };
        LogError = m => { Debug.WriteLine("Error: " + m); };
    }

    /// <summary>
    /// Does the weaving logic.
    /// </summary>
    public void Execute()
    {
        var msCoreLibDefinition = ModuleDefinition.AssemblyResolver.Resolve("mscorlib");
        var msCoreTypes = msCoreLibDefinition.MainModule.Types;
        var compilerGeneratedAttributeDefinition = msCoreTypes.First(x => x.Name == nameof(CompilerGeneratedAttribute));

        mCompilerGeneratedAttributeReference = ModuleDefinition.ImportReference(compilerGeneratedAttributeDefinition.Methods.First(x => x.IsConstructor));
        mTypeSystem = ModuleDefinition.TypeSystem;

        mISerializerTypeDefinition = ModuleDefinition.AssemblyResolver.Resolve(typeof(ISerializer).Namespace)
            .MainModule.GetType(typeof(ISerializer).FullName).Resolve();

        // Iterate thru all types in the assembly to be processed
        foreach (var typeDefinition in ModuleDefinition.GetTypes())
        {
            // take properties marked with attribute
            var serializableProperties = typeDefinition.Properties.Where(p => 
                p.CustomAttributes.Any(a => a.AttributeType.FullName == "EntityFramework.SerializableProperty.SerializablePropertyAttribute" /*typeof(SerializablePropertyAttribute).FullName*/))
                    .ToArray();

            foreach (var property in serializableProperties)
            {
                string propertyName = property.Name;

                LogInfo($"Processing property {propertyName}...");

                // Obtaining serializer class from the attribute
                CustomAttribute attr = property.CustomAttributes.First(a => a.AttributeType.FullName == "EntityFramework.SerializableProperty.SerializablePropertyAttribute" /*typeof(SerializablePropertyAttribute).FullName*/);
                var serializerTypeReference = (TypeReference)attr.ConstructorArguments[0].Value;
                var serializerTypeDefinition = serializerTypeReference.Resolve();

                // Check the serializer class implements ISerializer interface
                var doesSerializerImplementsInterface = serializerTypeDefinition.Interfaces.Any(tr => tr.FullName == "EntityFramework.SerializableProperty.ISerializer" /*typeof(ISerializer).FullName*/);
                if (!doesSerializerImplementsInterface)
                {
                    var message = $"Serializer type {serializerTypeDefinition.FullName} must implement {"EntityFramework.SerializableProperty.ISerializer" /*typeof(ISerializer).FullName*/} interface.";
                    LogError(message);
                    throw new ArgumentException(message);
                }

                // Check the serializer has parameterless constructor
                MethodReference serializerCtor = ModuleDefinition.ImportReference(serializerTypeDefinition.GetConstructors().First(c => c.Parameters.Count == 0));
                if (serializerCtor == null)
                {
                    var message = $"Serializer class {serializerTypeDefinition.FullName} must have parameterless constructor.";
                    LogError(message);
                    throw new ArgumentException(message);
                }

                // compose name for the new property
                var newPropertyName = $"{propertyName}_serialized";

                InjectionParameters parameters = new InjectionParameters()
                {
                    GeneratedPropertyName = newPropertyName,
                    SerializerType = serializerTypeDefinition,
                    SerializerConstructor = serializerCtor,
                    ProcessedProperty = property
                };
           
                var getter = ComposeGetter(parameters);
                var setter = ComposeSetter(parameters);

                var propertyDefinition = new PropertyDefinition(newPropertyName, PropertyAttributes.None, mTypeSystem.String)
                {
                    GetMethod = getter,
                    SetMethod = setter
                };

                propertyDefinition.CustomAttributes.Add(new CustomAttribute(mCompilerGeneratedAttributeReference));

                typeDefinition.Methods.Add(getter);
                typeDefinition.Methods.Add(setter);

                typeDefinition.Properties.Add(propertyDefinition);
            }
        }
    }

    /// <summary>
    /// Composes getter for the generated property
    /// </summary>
    /// <param name="parameters">InjectionParameters instance</param>
    /// <returns>MethodDefinition of the getter</returns>
    private MethodDefinition ComposeGetter(InjectionParameters parameters)
    {
        var method = new MethodDefinition("get_" + parameters.GeneratedPropertyName,
                                       MethodAttributes.Private | MethodAttributes.SpecialName |
                                       MethodAttributes.HideBySig, mTypeSystem.String);

        var instructions = method.Body.Instructions;
        method.Body.Variables.Add(new VariableDefinition(mTypeSystem.String));

        instructions.Add(Instruction.Create(OpCodes.Nop));
        instructions.Add(Instruction.Create(OpCodes.Newobj, parameters.SerializerConstructor));
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        instructions.Add(Instruction.Create(OpCodes.Call, parameters.ProcessedProperty.GetMethod));
        var baseSerializeMethod = ModuleDefinition.ImportReference(mISerializerTypeDefinition.Methods.First(m => m.Name == "Serialize" /*nameof(ISerializer.Serialize)*/));
        instructions.Add(Instruction.Create(OpCodes.Callvirt, baseSerializeMethod));
        instructions.Add(Instruction.Create(OpCodes.Stloc_0));
        var inst = Instruction.Create(OpCodes.Ldloc_0);
        instructions.Add(Instruction.Create(OpCodes.Br_S, inst));
        instructions.Add(inst);
        instructions.Add(Instruction.Create(OpCodes.Ret));
        method.Body.InitLocals = true;
        method.SemanticsAttributes = MethodSemanticsAttributes.Getter;

        return method;
    }

    /// <summary>
    /// Composes setter for the generated property
    /// </summary>
    /// <param name="parameters">InjectionParameters instance</param>
    /// <returns>MethodDefinition of the stter</returns>
    private MethodDefinition ComposeSetter(InjectionParameters parameters)
    {
        var method = new MethodDefinition("set_" + parameters.GeneratedPropertyName,
                                       MethodAttributes.Private | MethodAttributes.SpecialName |
                                       MethodAttributes.HideBySig, mTypeSystem.Void);

        method.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, mTypeSystem.String));

        var instructions = method.Body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Nop));
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        instructions.Add(Instruction.Create(OpCodes.Newobj, parameters.SerializerConstructor));
        instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        var baseDeserializeMethod = ModuleDefinition.ImportReference(mISerializerTypeDefinition.Methods.First(m => m.Name == "Deserialize" /*nameof(ISerializer.Deserialize)*/));
        var genericInstanceMethod = new GenericInstanceMethod(baseDeserializeMethod);
        genericInstanceMethod.GenericArguments.Add(parameters.ProcessedProperty.GetMethod.ReturnType);
        instructions.Add(Instruction.Create(OpCodes.Callvirt, genericInstanceMethod));
        instructions.Add(Instruction.Create(OpCodes.Call, parameters.ProcessedProperty.SetMethod));
        instructions.Add(Instruction.Create(OpCodes.Nop));
        instructions.Add(Instruction.Create(OpCodes.Ret));
        method.SemanticsAttributes = MethodSemanticsAttributes.Setter;

        return method;
    }

    /// <summary>
    /// Injection parameters.
    /// </summary>
    private class InjectionParameters
    {
        /// <summary>
        /// Original property to be processed
        /// </summary>
        public PropertyDefinition ProcessedProperty { get; set; }

        /// <summary>
        /// Type definition of a serializer
        /// </summary>
        public TypeDefinition SerializerType { get; set; }

        /// <summary>
        /// Serializer constructor method
        /// </summary>
        public MethodReference SerializerConstructor { get; set; }

        /// <summary>
        /// Name of the new generated property
        /// </summary>
        public string GeneratedPropertyName { get; set; }
    }
}