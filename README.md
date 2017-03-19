<img src="https://raw.github.com/AlexanderKrutov/EntityFramework.SerializableProperty.Fody/master/Icons/package_icon.png" width="96" height="96" />

# EntityFramework.SerializableProperty.Fody

### This is an add-in for [Fody](https://github.com/Fody/Fody/) 
Sometimes it's needed to have an ability to store complex data in serialized form with Entity Framework, despite of it is not supported out of the box. The most common way to do it is write a backing property mapped directly to a database column which will contain the serialized data (as JSON or XML string - does not really matter). Getter of the property will call a serializer and perform necessary serialization logic, and setter will do deserialize operation. Threre are two main disadvantagies of such approach: the backing  property should be public (in some cases it's undesirable) and necessity to write the explicit property's logic, that's lead to boilerplate coding.

The `EntityFramework.SerializableProperty.Fody` library solves the problem in a simple and elegant way. With usage of a technique named [IL weaving](http://stackoverflow.com/questions/189359/what-is-il-weaving) it adds **private** backing property with required getter and setter logic and maps database column to it.

## The nuget package  [![NuGet Status](http://img.shields.io/nuget/v/EntityFramework.SerializableProperty.Fody.svg?style=flat)](https://www.nuget.org/packages/EntityFramework.SerializableProperty.Fody/)

https://nuget.org/packages/EntityFramework.SerializableProperty.Fody/

    PM> Install-Package EntityFramework.SerializableProperty.Fody

## How to use
First, mark the complex property with `SerializableProperty` attribute. It tells to the Fody weaver that the backing property should be generated:
```csharp
public class PurchaseOrder
{
    ...
    [SerializableProperty(typeof(SimpleJsonSerializer))]
    public List<Product> Products { get; set; }
    ...
}
```
Single argument of the attribute is a type of a serializer to be used to serialize/deserialize the complex property. Your serializer should implement `ISerializer` interface with two methods:
```csharp
public interface ISerializer
{
    string Serialize(object obj);
    T Deserialize<T>(string str);
}
```
The last step is to configure the Entity Framework model type with calling `Serialized` extension method:
```csharp
modelBuilder.Entity<PurchaseOrder>()
    .ToTable("Orders")
    .Serialized(t => t.Products)
    .HasKey(t => t.Id);
```
By default, mapped database column name will equal to the property name.  
Optionally you can specify another column name by passing second argument to the extension method:
```csharp
    .Serialized(t => t.Products, "DifferentColumnName")
```

## What gets compiled
```csharp
public class PurchaseOrder
{
    ...
    public List<Product> Products { get; set; }
    ...
    [CompilerGenerated]
    private string Products_serialized
    {
        get
        {
            return ((ISerializer)new SimpleJsonSerializer()).Serialize(this.Products);
        }
        set
        {
            this.Products = ((ISerializer)new SimpleJsonSerializer()).Deserialize<List<Product>>(value);
        }
    }
    ...
}
```

## License
Licensed under [MIT license](LICENSE).

## Icon

<a href="https://thenounproject.com/term/ribbon/339989/" target="_blank">Ribbon</a> designed by <a href="
https://thenounproject.com/sandorsz/" target="_blank">Alexander Skowalsky</a> from the Noun Project.
