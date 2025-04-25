using BeautifulLyricsMobileV2.Pages;

namespace BeautifulLyricsMobileV2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            bool onboardingNeeded = !Preferences.Get("Onboarding", false);
            return new Window(onboardingNeeded ? new OnboardingPage() : new AppShell());
        }
    }
}