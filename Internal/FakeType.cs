using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

internal class FakeType : Type {

  public FakeType(string? ns, string name, Type? baseType = null, Assembly? assembly = null, Module? module = null, Guid? guid = null) {
    Namespace = ns;
    Name = name;
    assembly ??= new FakeAssembly(new($"FakeAssembly{Guid.NewGuid()}"));
    Assembly = assembly;
    Module = module ?? new FakeModule(assembly);
    var fullName = ns is null ? name : $"{ns}.{name}";
    FullName = fullName;
    AssemblyQualifiedName = $"{fullName}, {assembly.FullName}";
    BaseType = baseType;
    GUID = guid ?? Guid.NewGuid();
  }

  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public override object[] GetCustomAttributes(bool inherit)
    => Array.Empty<Attribute>();

  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    => (object[])Array.CreateInstance(attributeType, 0);

  public override IEnumerable<CustomAttributeData> CustomAttributes
    => Array.Empty<CustomAttributeData>();

  public override IList<CustomAttributeData> GetCustomAttributesData()
    => Array.Empty<CustomAttributeData>();

  public override StructLayoutAttribute? StructLayoutAttribute
    => null;

  public override bool IsDefined(Type attributeType, bool inherit)
    => throw new NotImplementedException();

  public override Module Module { get; }

  public override string? Namespace { get; }

  public override string Name { get; }

  protected override TypeAttributes GetAttributeFlagsImpl()
    => throw new NotImplementedException();

  protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
    => throw new NotImplementedException();

  public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override Type? GetElementType()
    => throw new NotImplementedException();

  public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override EventInfo[] GetEvents(BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override FieldInfo[] GetFields(BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
    => throw new NotImplementedException();

  protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
    => throw new NotImplementedException();

  public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
    => throw new NotImplementedException();

  public override Type UnderlyingSystemType
    => throw new NotImplementedException();

  protected override bool IsArrayImpl()
    => throw new NotImplementedException();

  protected override bool IsByRefImpl()
    => throw new NotImplementedException();

  protected override bool IsCOMObjectImpl()
    => throw new NotImplementedException();

  protected override bool IsPointerImpl()
    => throw new NotImplementedException();

  protected override bool IsPrimitiveImpl()
    => throw new NotImplementedException();

  public override Assembly Assembly { get; }

  public override string? AssemblyQualifiedName { get; }

  public override Type? BaseType { get; }

  public override string? FullName { get; }

  public override Guid GUID { get; }

  protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
    => throw new NotImplementedException();

  protected override bool HasElementTypeImpl()
    => throw new NotImplementedException();

  public override Type? GetNestedType(string name, BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override Type[] GetNestedTypes(BindingFlags bindingAttr)
    => throw new NotImplementedException();

  public override Type? GetInterface(string name, bool ignoreCase)
    => throw new NotImplementedException();

  public override Type[] GetInterfaces()
    => throw new NotImplementedException();

  public override int GetHashCode()
    => GUID.GetHashCode();

  public override string ToString()
    => FullName ?? Name;

}