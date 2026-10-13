namespace HangmeIfYouCan
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("statspage", typeof(StatsPage));
        }
    }
}
