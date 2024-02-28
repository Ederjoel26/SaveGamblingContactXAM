using Android.Gestures;
using SaveGamblingContactXAM.InterfaceServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SaveGamblingContactXAM
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

           
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            if(DependencyService.Resolve<ISignalRServices>().IsForegroundServiceRunning())
            {
                await DisplayAlert("El servicio en segundo plano ya se está ejecutando", "Si", "Si");
            }
            else
            {
                DependencyService.Resolve<ISignalRServices>().StartConnection();
            }
        }
    }
}
