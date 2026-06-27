#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure <see cref="JumpArc.Height"/> closed form: grounded before/after the leap, the curve sample
    /// in flight, the duration guard, and linear height scaling. A known <see cref="AnimationCurve"/> pins
    /// the t-mapping (the production curve is a hump; here a linear ramp makes the arithmetic exact).
    /// </summary>
    public sealed class JumpArcTests
    {
        private const float Tolerance = 1e-4f;

        private static AnimationCurve Linear()
        {
            return AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        [Test]
        public void Grounded_BeforeLeap_IsZero()
        {
            // The NegativeInfinity sentinel lands on the t >= 1 branch (t → +inf), also returning 0.
            Assert.That(JumpArc.Height(0f, float.NegativeInfinity, 0.45f, 0.5f, Linear()), Is.EqualTo(0f));
            Assert.That(JumpArc.Height(1f, 2f, 0.45f, 0.5f, Linear()), Is.EqualTo(0f));
        }

        [Test]
        public void LiftOff_AtLeapStart_IsCurveZeroTimesHeight()
        {
            // t == 0 → Linear(0) == 0 (and the production hump is also 0 at the ends).
            Assert.That(JumpArc.Height(2f, 2f, 0.4f, 0.5f, Linear()), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void MidLeap_LinearCurve_IsHalfHeightTimesHeight()
        {
            // now = leapStart + duration/2 → t = 0.5 → Linear(0.5) * height = 0.5 * 0.5.
            Assert.That(JumpArc.Height(2.2f, 2f, 0.4f, 0.5f, Linear()), Is.EqualTo(0.25f).Within(Tolerance));
        }

        [Test]
        public void Landed_AtOrAfterDuration_IsZero()
        {
            Assert.That(JumpArc.Height(2.4f, 2f, 0.4f, 0.5f, Linear()), Is.EqualTo(0f));
            Assert.That(JumpArc.Height(5f, 2f, 0.4f, 0.5f, Linear()), Is.EqualTo(0f));
        }

        [Test]
        public void NonPositiveDuration_IsZero()
        {
            Assert.That(JumpArc.Height(2.2f, 2f, 0f, 0.5f, Linear()), Is.EqualTo(0f));
            Assert.That(JumpArc.Height(2.2f, 2f, -1f, 0.5f, Linear()), Is.EqualTo(0f));
        }

        [Test]
        public void Height_ScalesLinearly()
        {
            float h1 = JumpArc.Height(2.2f, 2f, 0.4f, 0.5f, Linear());
            float h2 = JumpArc.Height(2.2f, 2f, 0.4f, 1.0f, Linear());
            Assert.That(h2, Is.EqualTo(h1 * 2f).Within(Tolerance));
        }
    }
}
