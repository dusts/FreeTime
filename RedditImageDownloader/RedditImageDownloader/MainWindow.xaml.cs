using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RedditSharp;
using RedditSharp.Things;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Imgur.API.Models;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using System.Windows.Threading;

namespace RedditImageDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SolidColorBrush red = new SolidColorBrush(Colors.Red);
        SolidColorBrush green = new SolidColorBrush(Colors.Green);
        SolidColorBrush black = new SolidColorBrush(Colors.Black);
        FromTime fromtime;
        List<Uri> imageUrlList = new List<Uri>();
        List<IImage> imageLinkList = new List<IImage>();
        int counter;
        string ImgurClientID = "04cc5217fd576cb";
        public MainWindow()
        {
            //Redesign with asynchronous in mind
            //implement filtering by .png .jpg /a/ (albums) etc
            InitializeComponent();
        }
        public void GetTopLinks(string name, FromTime ft, int howMany)
        {
            DebugLog.Text += $"Searching {name} for {howMany} top posts from past {ft} ago\n";
            Reddit reddit = new Reddit();
            Subreddit subreddit = reddit.GetSubreddit(name);
            int count = 1;
            int newItems = 0;
            foreach (var post in subreddit.GetTop(ft))
            {
                if (checkBox_FindNewItems.IsChecked == false)
                {
                    count++;
                    if (!imageUrlList.Contains(post.Url))
                    {
                        DebugLog.Text += post.Title + "\n" + post.Url + "\n";
                        imageUrlList.Add(post.Url);
                        newItems++;
                    }
                    if (count > howMany)
                    {
                        DebugLog.Text += $"Found {newItems} new items!\n";
                        break;
                    }
                }
                else
                {
                    if (!imageUrlList.Contains(post.Url))
                    {
                        DebugLog.Text += post.Title + "\n" + post.Url + "\n";
                        imageUrlList.Add(post.Url);
                        count++;
                        newItems++;
                    }
                    if (count > howMany)
                    {
                        DebugLog.Text += $"Found {newItems} new items!\n";
                        break;
                    }
                }
            }
            TheGreatImageFilter(imageUrlList);
            DebugLog.Text += $"\n\n";
        }
        public void TheGreatImageFilter(List<Uri> uriList)
        {
            string uriString;
            if (uriList[0] != null)
            {
                foreach (Uri item in uriList)
                {
                    uriString = item.ToString();
                    if (LinkHasExtension(uriString))
                    {
                        if (uriString.Contains(".jpg"))
                        {
                            DebugLog.Text += "JPG PICTURE \n";
                        }
                        else if (uriString.Contains(".png"))
                        {
                            DebugLog.Text += "PNG PICTURE \n";
                        }
                    }
                    else if (LinkIsImgurAlbum(uriString))
                    {
                        DebugLog.Text += "New album detected! \n";
                        uriString = uriString.Substring(uriString.LastIndexOf('/') + 1);
                        ImgurAlbumAsyncWrapper imgurAlbumAsyncWrapper = new ImgurAlbumAsyncWrapper();
                        imgurAlbumAsyncWrapper.NewLinksFromImgurAlbum += UpdateImageLinksList_NewLinksFromImgurAlbum;
                        imgurAlbumAsyncWrapper.FindImagesInAlbumAsync(ImgurClientID, uriString);
                    }
                    else if (LinkIsImgurGallery(uriString))
                    {
                        DebugLog.Text += "New gallery detected! \n";
                        uriString = uriString.Substring(uriString.LastIndexOf('/') + 1);
                        ImgurGalleryAsyncWrapper imgurGalleryAsyncWrapper = new ImgurGalleryAsyncWrapper();
                        imgurGalleryAsyncWrapper.NewLinksFromImgurGallery += UpdateImageLinksList_NewLinksFromImgurGallery;
                        imgurGalleryAsyncWrapper.FindImagesInGalleryAsync(ImgurClientID, uriString);
                    }
                    else if (LinkIsImgurImage(uriString))
                    {
                        DebugLog.Text += "New image detected! \n";
                        uriString = uriString.Substring(uriString.LastIndexOf('/') + 1);
                        ImgurImageAsyncWrapper imgurGalleryAsyncWrapper = new ImgurImageAsyncWrapper();
                        imgurGalleryAsyncWrapper.NewLinksFromImgurImage += UpdateImageLinksList_NewLinkFromImgurImage;
                        imgurGalleryAsyncWrapper.FindImageInLinkAsync(ImgurClientID, uriString);
                    }
                    else
                    {
                        DebugLog.Text += $"The link is in invalid format! - {item}\n";
                    }
                }
            }
        }
        public bool LinkHasExtension(string uriString)
        {
            if (uriString.Contains(".jpg") || uriString.Contains(".png"))
            {
                return true;
            }
            return false;
        }
        public bool LinkIsImgurAlbum(string uriString)
        {
            if (uriString.Contains("/a/"))
            {
                return true;
            }
            return false;
        }
        public bool LinkIsImgurGallery(string uriString)
        {
            if (uriString.Contains("/gallery/"))
            {
                return true;
            }
            return false;
        }
        public bool LinkIsImgurImage(string uriString)
        {
            if (uriString.Contains("imgur"))
            {
                return true;
            }
            return false;

        }
        public void DownloadImageWithExtension(Uri url, ImageFormat format)
        {
            using (WebClient client = new WebClient())
            {
                //client.DownloadFile(new Uri(url), @"c:\temp\image35.png");
                //OR 
                client.DownloadFileAsync(url, $@"c:\\Users\Tomats\Desktop\Wallpapers\{url}.{format}");
            }
        }
        public async Task<bool> CheckSubredditAsync(string name)
        {
            try
            {
                Reddit reddit = new Reddit();
                Subreddit subreddit = await reddit.GetSubredditAsync(name);
                var post = subreddit.New.Take(1);
                return true;
            }
            catch 
            {
                return false;
            }
        }
        private async void btn_CheckSubreddit_Click(object sender, RoutedEventArgs e)
        {
            btn_CheckSubreddit.IsEnabled = false;
            string newText = textB_SubredditInput.Text;
            if (await CheckSubredditAsync(newText))
            {
                btn_CheckSubreddit.Content = "Exists";
                btn_CheckSubreddit.Foreground = green;
            }
            else
            {
                btn_CheckSubreddit.Content = "Doesn't Exist";
                btn_CheckSubreddit.Foreground = red;
            }
            btn_CheckSubreddit.IsEnabled = true;
        }

        private void textB_SubredditInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btn_CheckSubreddit != null)
            {
                string text = btn_CheckSubreddit.Content.ToString();
                if (text != "Check Again" && text != "Check")
                {
                    btn_CheckSubreddit.Foreground = black;
                    btn_CheckSubreddit.Content = "Check Again";
                }
            }
        }

        private void slider_PostCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (label_PostCount_Number != null && slider_PostCount != null)
            {
                label_PostCount_Number.Content = slider_PostCount.Value;
            }   
        }

        private void btn_SearchSubreddit_Click(object sender, RoutedEventArgs e)
        {
            if ((string)btn_CheckSubreddit.Content == "Exists" && listBox_FromTime.SelectedItem != null)
            {
                textBlock_ErrorMessageForSearchBtn.Text = "Wait!";
                GetTopLinks(textB_SubredditInput.Text, fromtime, Convert.ToInt16(label_PostCount_Number.Content));
                textBlock_ErrorMessageForSearchBtn.Text = "Done!";
            }
            else
            {
                if ((string)btn_CheckSubreddit.Content != "Exists")
                {
                    textBlock_ErrorMessageForSearchBtn.Text = "Find the right subreddit!";
                }
                else if (listBox_FromTime.SelectedItem == null)
                {
                    textBlock_ErrorMessageForSearchBtn.Text = "Select an item from the List Box!";
                }
            }
        }

        private void listBox_FromTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedTime = ((ListBoxItem)listBox_FromTime.SelectedValue).Content.ToString();
            switch (selectedTime)
            {
                case "Day":
                    fromtime = FromTime.Day;
                    break;
                case "Week":
                    fromtime = FromTime.Week;
                    break;
                case "Month":
                    fromtime = FromTime.Month;
                    break;
                case "Year":
                    fromtime = FromTime.Year;
                    break;
                case "All":
                    fromtime = FromTime.All;
                    break;
                default:
                    break;
            }
        }
        #region Update Debug.Text Methods
        void UpdateImageLinksList_NewLinksFromImgurAlbum(List<IImage> iimageList)
        {
            this.Dispatcher.Invoke(() =>
            {
                counter = 1;
                DebugLog.Text += "New Album Links Added\n";
                imageLinkList.AddRange(iimageList);
                //foreach (var item in albumImageList)
                //{
                //    DebugLog.Text += $"{item.Link} - {counter}\n";
                //    counter++;
                //}
            });
        }
        void UpdateImageLinksList_NewLinksFromImgurGallery(List<IImage> iimageList)
        {
            this.Dispatcher.Invoke(() =>
            {
                counter = 1;
                DebugLog.Text += "New Gallery Links Added\n";
                imageLinkList.AddRange(iimageList);
                //foreach (var item in albumImageList)
                //{
                //    DebugLog.Text += $"{item.Link} - {counter}\n";
                //    counter++;
                //}
            });
        }
        void UpdateImageLinksList_NewLinkFromImgurImage(IImage iimageList)
        {
            this.Dispatcher.Invoke(() =>
            {
                counter = 1;
                DebugLog.Text += "New Image Link Added\n";
                imageLinkList.Add(iimageList);
                //foreach (var item in albumImageList)
                //{
                //    DebugLog.Text += $"{item.Link} - {counter}\n";
                //    counter++;
                //}
            });
        }
        #endregion
    }
    public delegate void NewLinksFromImgurAlbumDelegate(List<IImage> iimageList);
   
    public class ImgurAlbumAsyncWrapper
    {
        public List<IImage> iimage;
        string token;
        string albumlink;
        public event NewLinksFromImgurAlbumDelegate NewLinksFromImgurAlbum;
        public async void FindImagesInAlbumAsync(string token, string albumlink)
        {
            this.token = token;
            this.albumlink = albumlink;
            iimage = await Task.Run(GetIenumerableIImagesAsync);
            NewLinksFromImgurAlbum(iimage);
        }
        public async Task<List<IImage>> GetIenumerableIImagesAsync()
        {
            IEnumerable<IImage> image = await Task.Run(AccessTheWebAsync);
            return image.ToList();
        }
        async Task<IEnumerable<IImage>> AccessTheWebAsync()
        {
            var client = new ImgurClient(token);
            var endpoint = new AlbumEndpoint(client);
            var images = await endpoint.GetAlbumImagesAsync(albumlink);
            return images;
        }
    }
    public delegate void NewLinksFromImgurGalleryDelegate(List<IImage> iimageList);
    public class ImgurGalleryAsyncWrapper
    {
        public List<IImage> iimage;
        public List<IImage> iimage2;
        string token;
        string gallerylink;
        public event NewLinksFromImgurGalleryDelegate NewLinksFromImgurGallery;
        public async void FindImagesInGalleryAsync(string token, string gallerylink)
        {
            //Using try and catch because sometimes the gallery doesn't have an album inside it. No idea how to get the Images then.
            try
            {
                this.token = token;
                this.gallerylink = gallerylink;
                iimage = await Task.Run(GetIenumerableIImagesAsync);
                NewLinksFromImgurGallery(iimage);
            }
            catch
            {

            }
        }
        public async Task<List<IImage>> GetIenumerableIImagesAsync()
        {
            IEnumerable<IImage> image = await Task.Run(AccessTheWebAsync);
            return image.ToList(); ;
        }
        async Task<IEnumerable<IImage>> AccessTheWebAsync()
        {
            var client = new ImgurClient(token);
            var endpoint = new GalleryEndpoint(client);
            var images = await endpoint.GetGalleryAlbumAsync(gallerylink);
            return images.Images;
        }
    }
    public delegate void NewLinkFromImgurImageDelegate(IImage iimage);
    public class ImgurImageAsyncWrapper
    {
        string token;
        string gallerylink;
        public event NewLinkFromImgurImageDelegate NewLinksFromImgurImage;
        public async void FindImageInLinkAsync(string token, string gallerylink)
        {
            this.token = token;
            this.gallerylink = gallerylink;
            NewLinksFromImgurImage(await Task.Run(AccessTheWebAsync));
        }
        async Task<IImage> AccessTheWebAsync()
        {
            var client = new ImgurClient(token);
            var endpoint = new ImageEndpoint(client);
            var images = await endpoint.GetImageAsync(gallerylink);
            return images;
        }
    }
}
