// Minimal NUnit-compatible surface for running PKR EditMode tests outside Unity.
// Only what the tests use. Semantics match NUnit 3 classic asserts.
using System;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class TestFixtureAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class SetUpAttribute : Attribute { }

    public delegate void TestDelegate();

    public class AssertionException : Exception { public AssertionException(string m) : base(m) { } }

    public static class Assert
    {
        static void Fail(string msg, string user) => throw new AssertionException(user == null ? msg : msg + " | " + user);

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!Equals(expected, actual)) Fail($"Expected {expected} but was {actual}", message);
        }
        public static void AreEqual(double expected, double actual, double delta, string message = null)
        {
            if (double.IsNaN(actual) || Math.Abs(expected - actual) > delta) Fail($"Expected {expected} +/- {delta} but was {actual}", message);
        }
        public static void IsTrue(bool c, string message = null) { if (!c) Fail("Expected true", message); }
        public static void IsFalse(bool c, string message = null) { if (c) Fail("Expected false", message); }
        public static void IsNull(object o, string message = null) { if (o != null) Fail($"Expected null but was {o}", message); }
        public static void IsNotNull(object o, string message = null) { if (o == null) Fail("Expected not null", message); }
        public static void Less(float a, float b, string message = null) { if (!(a < b)) Fail($"Expected {a} < {b}", message); }
        public static void Greater(float a, float b, string message = null) { if (!(a > b)) Fail($"Expected {a} > {b}", message); }
        public static void LessOrEqual(float a, float b, string message = null) { if (!(a <= b)) Fail($"Expected {a} <= {b}", message); }
        public static void AreNotEqual(object notExpected, object actual, string message = null)
        {
            if (Equals(notExpected, actual)) Fail($"Expected anything but {notExpected}", message);
        }
        public static T Throws<T>(TestDelegate code) where T : Exception
        {
            try { code(); }
            catch (T ex) when (ex.GetType() == typeof(T)) { return ex; }
            catch (Exception ex) { Fail($"Expected {typeof(T).Name} but got {ex.GetType().Name}", null); }
            Fail($"Expected {typeof(T).Name} but nothing was thrown", null);
            return null;
        }
    }
}
