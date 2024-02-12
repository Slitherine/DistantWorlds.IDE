using System.Reflection;

internal class FakeAssembly : Assembly {

  public FakeAssembly(AssemblyName assemblyName, string? location = null) {
    AssemblyName = assemblyName;
    Location = location ?? "";
  }

  public override string Location { get; }

  [Obsolete("Obsolete", true)]
  public override string? CodeBase => Location;

  [Obsolete("Obsolete", true)]
  public override string EscapedCodeBase => Uri.EscapeDataString(Location);

  public AssemblyName AssemblyName { get; }

  public override string FullName => AssemblyName.FullName;

  public override AssemblyName GetName()
    => AssemblyName;

  public override AssemblyName GetName(bool copiedName)
    => new(AssemblyName.ToString());

  public override int GetHashCode()
    => AssemblyName.GetHashCode();

  public override string ToString()
    => AssemblyName.ToString();

}