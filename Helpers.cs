using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using DistantWorlds.IDE.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.VisualStudio.Threading;

namespace DistantWorlds.IDE;

[PublicAPI]
public static class Helpers {

  // ReSharper disable once IdentifierTypo
  public static JoinableTaskContext SharedJoinableTaskContext = new JoinableTaskContext();

  // ReSharper disable once IdentifierTypo
  public static readonly JoinableTaskFactory SharedJoinableTaskFactory = SharedJoinableTaskContext.Factory;

  public static readonly ILoggerProvider SharedConsoleLoggerProvider
    = new ConsoleLoggerProvider(new ConsoleOptionsMonitor());

  public static readonly ILoggerFactory SharedConsoleLoggerFactory
    = new LoggerFactory(
      new[] { SharedConsoleLoggerProvider },
#if DEBUG
      new LoggerFilterOptions { MinLevel = LogLevel.Trace }
#else
      new LoggerFilterOptions { MinLevel = LogLevel.Warning }
#endif
    );

  /// <summary>
  /// Determines if a virtual method is overridden in a derived type.
  /// This also checks the hierarchy between the two types.
  /// </summary>
  /// <param name="type">The the type to check.</param>
  /// <param name="method">The base implementation of the virtual method.</param>
  /// <returns></returns>
  public static bool Overrides(this Type type, MethodInfo method) {
    if (method.IsGenericMethod || method.ContainsGenericParameters)
      throw new NotImplementedException();

    var sourceType = method.ReflectedType;
    if (sourceType is null) return false;
    if (!sourceType.IsAssignableFrom(type))
      return false; // not a derived type");

    if (sourceType.IsInterface)
      // it has to be an interface method, has to be overridden
      return true; // interface methods must be overridden

    // if virtual or abstract, must be overridden in the hierarchy
    if (method is { IsVirtual: false, IsAbstract: false })
      return false; // not virtual or abstract

    var bindingFlags = (method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic)
      | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
    var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
    var impl = type.GetMethod(method.Name, bindingFlags, paramTypes);
    if (impl is null)
      return false; // not found

    if (impl.IsSameAs(method))
      return false; // same method

    if (impl is not { IsVirtual: true })
      return false; // new slot

    var implBase = impl.GetBaseDefinition();
    if (implBase.IsSameAs(method))
      return true; // direct override

    var methodBase = method.GetBaseDefinition();
    // true: descendant override
    // false: no common base method
    return methodBase.IsSameAs(implBase);
  }

  /// <summary>
  /// Two methods can be the same but their equal operators may
  /// return false because they are not the same data; they may
  /// have different reflection contexts.
  /// (e.g. <see cref="MethodInfo.ReflectedType" /> may differ.)
  /// </summary>
  /// <param name="lhs">Left hand side method to check.</param>
  /// <param name="rhs">Right hand side method to check.</param>
  /// <returns><see langword="true"/> if equal, otherwise <see langword="false"/>.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsSameAs(this MethodBase lhs, MethodBase rhs)
    => lhs.MethodHandle == rhs.MethodHandle;

  private static MethodInfo ExtractMethod(LambdaExpression expr) {
    if (expr.Body is UnaryExpression un)
      return un.Method!;

    if (expr.Body is BinaryExpression bin)
      return bin.Method!;

    if (expr.Body is MethodCallExpression call)
      return call.Method;

    throw new ArgumentException("Can't locate a method call in the expression.", nameof(expr));
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static MethodInfo Method<T, TResult>(Expression<Func<T, TResult>> expr)
    => ExtractMethod(expr);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static MethodInfo Method(Expression<Action> expr)
    => ExtractMethod(expr);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static MethodInfo Method<T>(Expression<Action<T>> expr)
    => ExtractMethod(expr);

  private static ConstructorInfo ExtractConstructor(LambdaExpression expr) {
    if (expr.Body is NewExpression newExpr)
      return newExpr.Constructor!;

    if (expr.Body is NewExpression call)
      return call.Constructor!;

    throw new ArgumentException("Can't locate a constructor in the expression.", nameof(expr));
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ConstructorInfo Constructor<T>(Expression<Func<T>> expr)
    => ExtractConstructor(expr);

  private static readonly MethodInfo ObjectToString = Method<object, string?>(o => o.ToString());

  public static unsafe string Stringify(object? x, bool fromTypeAndPointerOnly = false) {
    if (x is null)
      return "null";

    var type = x.GetType();

    var p = (ulong)Unsafe.AsPointer(ref x);

    if (type == typeof(object))
      return $"[Object 0x{p:X8}]";

    if (fromTypeAndPointerOnly)
      return $"[{type.Name} 0x{p:X8}]";

    var str = x as string;
    if (str is null && type.Overrides(ObjectToString))
      str = x.ToString();
    return str ?? $"[{type.Name} 0x{p:X8}]";
  }

  public static string CsvEscape(string? str) {
    if (str is null) return "";
    str = str.Replace("\"", "\"\"");
    if (str.Contains(',')) str = $"\"{str}\"";
    return str;
  }

  public static string JsonEscape(string? str) {
    if (str is null) return "";
    str = str.Replace("\\", "\\\\");
    str = str.Replace("\"", "\\\"");
    str = str.Replace("\b", "\\b");
    str = str.Replace("\f", "\\f");
    str = str.Replace("\n", "\\n");
    str = str.Replace("\r", "\\r");
    str = str.Replace("\t", "\\t");
    return str;
  }

}