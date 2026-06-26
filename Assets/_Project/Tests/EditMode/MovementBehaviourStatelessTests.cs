#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using ZooWorld.Config;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// Architecture guard (ADR 0002 / guardrails §5): every concrete movement strategy must be stateless
    /// — no mutable instance fields — so a shared SO asset can be ticked by many animals safely.
    /// </summary>
    public sealed class MovementBehaviourStatelessTests
    {
        [Test]
        public void AllMovementBehaviours_HaveNoMutableInstanceFields()
        {
            Type baseType = typeof(MovementBehaviour);
            Type[] all = baseType.Assembly.GetTypes();

            foreach (Type type in all)
            {
                if (type.IsAbstract || !baseType.IsAssignableFrom(type))
                {
                    continue;
                }

                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    Assert.That(field.IsInitOnly || field.IsLiteral, Is.True,
                        type.Name + "." + field.Name + " is a mutable instance field — strategies must be stateless.");
                }
            }
        }
    }
}
