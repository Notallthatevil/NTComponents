namespace NTComponents.Tests.TestingUtility {

    /// <summary>
    ///     Provides test helpers for JSInterop setup.
    /// </summary>
    public static class TestingUtility {

        /// <summary>
        ///     Sets up the ripple effect JSInterop module and its methods for bUnit tests.
        /// </summary>
        /// <param name="testContext">The bUnit test context.</param>
        public static void SetupRippleEffectModule(BunitContext testContext) {
            var rippleModule = testContext.JSInterop.SetupModule("./_content/NTComponents/Core/TnTRippleEffect.razor.js");
            rippleModule.SetupVoid("onLoad", _ => true);
            rippleModule.SetupVoid("onUpdate", _ => true);
            rippleModule.SetupVoid("onDispose", _ => true);
        }
    }
}