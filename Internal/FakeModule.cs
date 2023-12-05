using System.Reflection;

internal class FakeModule : Module {

  public FakeModule(string name, string? path = null, Assembly? assembly = null) {
    Name = name;
    Assembly = assembly ?? new FakeAssembly(new(name));
    FullyQualifiedName = path ?? name;
  }

  public FakeModule(Assembly assembly, string? path = null) {
    Assembly = assembly;
    var name = assembly.GetName().Name;
    Name = name!;
    FullyQualifiedName = path ?? name!;
  }

  public override string Name { get; }

  public override Assembly Assembly { get; }

  public override string FullyQualifiedName { get; }

  
  public override int GetHashCode()
    => FullyQualifiedName.GetHashCode();
  
  public override string ToString()
    => FullyQualifiedName;
}