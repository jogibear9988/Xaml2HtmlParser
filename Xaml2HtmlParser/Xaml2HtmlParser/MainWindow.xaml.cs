using System.Windows;
using System.Windows.Input;
using CefSharp;

namespace Xaml2HtmlParser
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        public MainWindow()
        {
            InitializeComponent();

	        
			
		}

	    private void TextXaml_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
	    {
			var frame = viewHtml.GetMainFrame();

			//Create a new request knowing we'd like to use PostData
			var request = frame.CreateRequest(initializePostData: true);
			request.Method = "POST";
			request.Url = "www.google.de";
			request.PostData.AddData("test=123&data=456");

			frame.LoadRequest(request);
		}
    }
}
