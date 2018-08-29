using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PermissionApp
{
    public partial class MainPage : ContentPage
    {
        async void Handle_Clicked_1(object sender, System.EventArgs e)
        {
            var idService = DependencyService.Get<IIdentityService>();

            await idService.Login();
        }

        async void Handle_Clicked(object sender, System.EventArgs e)
        {
            var dataService = new DataService();

            var reviews = await dataService.LoadReviews();

            foreach (var review in reviews)
            {
                theDataLabel.Text += review.ReviewText;
                theDataLabel.Text += Environment.NewLine;
                theDataLabel.Text += Environment.NewLine;
            }
        }

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
