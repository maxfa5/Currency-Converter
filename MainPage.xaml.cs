namespace Converter
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            var cacheService = new CacheService();
            var currencyService = new CurrencyService(cacheService);

            BindingContext = new CurrencyConverterViewModel(currencyService, cacheService);
        }

    }

}
