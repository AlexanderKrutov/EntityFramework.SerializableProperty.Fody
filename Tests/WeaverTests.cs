using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;
using System.Linq;

[TestFixture]
public class WeaverTests
{
    Assembly assembly;
    string newAssemblyPath;
    string assemblyPath;
    const string GENERATED_PROPERTY_NAME = "Products_serialized";

    [OneTimeSetUp]
    public void AllTestsSetup()
    {
        Environment.CurrentDirectory = Path.GetDirectoryName(typeof(WeaverTests).Assembly.Location);

        var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
        assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");

#if (!DEBUG)
        assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Release\AssemblyToProcess.dll");
#endif

        newAssemblyPath = assemblyPath.Replace(".dll", "2.dll");
        File.Copy(assemblyPath, newAssemblyPath, true);

        var moduleDefinition = ModuleDefinition.ReadModule(newAssemblyPath);
        var weavingTask = new ModuleWeaver
        {
            ModuleDefinition = moduleDefinition
        };

        weavingTask.Execute();
        moduleDefinition.Write(newAssemblyPath);
        assembly = Assembly.LoadFile(newAssemblyPath);

        
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();
        var referencedPaths = Directory.GetFiles(Path.GetDirectoryName(assemblyPath), "*.dll");
        var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();
        toLoad.ForEach(path => AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
    }

    [Test]
    public void ValidatePropertyIsInjected()
    {
        var type = assembly.GetType("AssemblyToProcess.PurchaseOrder");
        var instance = (dynamic)Activator.CreateInstance(type);
        Assert.NotNull(GetProperty(instance, GENERATED_PROPERTY_NAME));
    }

    [Test]
    public void ValidateGetter()
    {
        var orderType = assembly.GetType("AssemblyToProcess.PurchaseOrder");
        var order = (dynamic)Activator.CreateInstance(orderType);

        var productType = assembly.GetType("AssemblyToProcess.Product");

        var milk = (dynamic)Activator.CreateInstance(productType);
        milk.Quantity = 1;
        milk.Name = "Milk";
        milk.Price = 60.0m;

        var potato = (dynamic)Activator.CreateInstance(productType);
        potato.Quantity = 2;
        potato.Name = "Potato";
        potato.Price = 25.0m;

        order.Products.Add(milk);
        order.Products.Add(potato);

        Assert.AreEqual("[{\"Price\":60.0,\"Name\":\"Milk\",\"Quantity\":1},{\"Price\":25.0,\"Name\":\"Potato\",\"Quantity\":2}]", 
            GetPropertyValue(order, GENERATED_PROPERTY_NAME));
    }

    [Test]
    public void ValidateSetter()
    {
        var type = assembly.GetType("AssemblyToProcess.PurchaseOrder");
        var instance = (dynamic)Activator.CreateInstance(type);

        SetPropertyValue(instance, GENERATED_PROPERTY_NAME, 
            "[{\"Price\":60.0,\"Name\":\"Milk\",\"Quantity\":1},{\"Price\":25.0,\"Name\":\"Potato\",\"Quantity\":2}]");

        Assert.AreEqual(2, instance.Products.Count);
        Assert.AreEqual("Milk", instance.Products[0].Name);
        Assert.AreEqual(60.0m, instance.Products[0].Price);
        Assert.AreEqual(1, instance.Products[0].Quantity);

        Assert.AreEqual("Potato", instance.Products[1].Name);
        Assert.AreEqual(25.0m, instance.Products[1].Price);
        Assert.AreEqual(2, instance.Products[1].Quantity);
    }

    [Test]
    public void TestEF()
    {
        var repository = assembly.GetType("AssemblyToProcess.Repository");

        var orderType = assembly.GetType("AssemblyToProcess.PurchaseOrder");
        var order = (dynamic)Activator.CreateInstance(orderType);

        var productType = assembly.GetType("AssemblyToProcess.Product");

        var milk = (dynamic)Activator.CreateInstance(productType);
        milk.Quantity = 1;
        milk.Name = "Milk";
        milk.Price = 60.0m;

        var potato = (dynamic)Activator.CreateInstance(productType);
        potato.Quantity = 2;
        potato.Name = "Potato";
        potato.Price = 25.0m;

        order.Products.Add(milk);
        order.Products.Add(potato);

        // clear database
        repository.GetMethod("ClearOrders").Invoke(null, null);

        // add order to database
        repository.GetMethod("AddOrder").Invoke(null, new object[] { order });

        // try to get orders from database
        var list = (dynamic)repository.GetMethod("GetOrders").Invoke(null, null);

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(order.Products.Count, list[0].Products.Count);
        Assert.AreEqual(order.Products[0].Name, list[0].Products[0].Name);
        Assert.AreEqual(order.Products[0].Quantity, list[0].Products[0].Quantity);
        Assert.AreEqual(order.Products[0].Price, list[0].Products[0].Price);
        Assert.AreEqual(order.Products[1].Name, list[0].Products[1].Name);
        Assert.AreEqual(order.Products[1].Quantity, list[0].Products[1].Quantity);
        Assert.AreEqual(order.Products[1].Price, list[0].Products[1].Price);
    }

    #if (DEBUG)
    [Test]
    public void PeVerify()
    {
        Verifier.Verify(assemblyPath, newAssemblyPath);
    }
    #endif

    private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        return AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName == args.Name);
    }

    private PropertyInfo GetProperty(object src, string propName)
    {
        return src.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private dynamic GetPropertyValue(object src, string propName)
    {
        return (dynamic)GetProperty(src, propName).GetValue(src, null);
    }

    private void SetPropertyValue(object src, string propName, object value)
    {
        GetProperty(src, propName).SetValue(src, value, null);
    }
}