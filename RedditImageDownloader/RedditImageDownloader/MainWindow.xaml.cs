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
        Dictionary<string, Uri> imageUrlDict = new Dictionary<string, Uri>();
        Dictionary<string, List<Uri>> imageLinkDict = new Dictionary<string, List<Uri>>();
        string ImgurClientID = "04cc5217fd576cb";
        bool breakLinkSearching = false;
        public MainWindow()
        {
            //Redesign with asynchronous in mind
            InitializeComponent();
        }
        public Task GetTopLinksAsync(string name, FromTime ft , int howMany)
        {
            return Task.Run(() => GetTopLinks(name, ft, howMany));
        }
        public void GetTopLinks(string name, FromTime ft, int howMany)
        {
            imageUrlDict = new Dictionary<string, Uri>();
            WriteToDebugLog($"Searching {name} for {howMany} top posts from past {ft} ago");
            Reddit reddit = new Reddit();
            Subreddit subreddit = reddit.GetSubreddit(name);
            int count = 1;
            int newItems = 0;
            foreach (Post post in subreddit.GetTop(ft))
            {
                breakLinkSearching = false;
                var findNewItems_IsChecked = false;
                this.Dispatcher.Invoke(() =>
                {
                    findNewItems_IsChecked = checkBox_FindNewItems.IsChecked ?? false;
                });

                AddPostToList(ref count, ref newItems, post, findNewItems_IsChecked, howMany);
                if (breakLinkSearching)
                    break;
            }
            TheGreatImageFilter(imageUrlDict);
            WriteToDebugLog("\n");
        }
        private void AddPostToList(ref int count, ref int newPosts, Post post, bool countInside, int howMany)
        {
            if (!countInside)
            {
                count++;
            }
            if (!imageUrlDict.ContainsKey(post.Title) && !imageLinkDict.ContainsKey(post.Title))
            {
                WriteToDebugLog($"{post.Title} \n{post.Url} \n");
                imageUrlDict.Add(post.Title, post.Url);
                newPosts++;
                if (countInside)
                    count++;
            }
            if (count > howMany)
            {
                int newPostsInside = newPosts;
                WriteToDebugLog($"Found {newPostsInside} new items!\n");
                breakLinkSearching = true;
            }
        }
        public void TheGreatImageFilter(Dictionary<string, Uri> imageUrlDict)
        {
            string uriString;
            if (imageUrlDict != null)
            {
                foreach (var pair in imageUrlDict)
                {
                    uriString = pair.Value.ToString();
                    if (LinkHasExtension(uriString))
                    {
                        imageLinkDict.Add(pair.Key, new List<Uri> { pair.Value });
                    }
                    else if (LinkIsImgurAlbum(uriString))
                    {
                        WriteToDebugLog("New album detected! \n");
                        uriString = uriString.Substring(uriString.LastIndexOf('/') + 1);
                        ImgurAlbumAsyncWrapper imgurAlbumAsyncWrapper = new ImgurAlbumAsyncWrapper();
                        imgurAlbumAsyncWrapper.NewLinksFromImgurAlbum += UpdateImageLinksList_NewLinksFromImgurAlbum;
                        imgurAlbumAsyncWrapper.FindImagesInAlbumAsync(ImgurClientID, uriString, pair.Key);
                    }
                    else if (LinkIsImgurGallery(uriString))
                    {
                        WriteToDebugLog("New gallery detected! \n");
                        uriString = uriString.Substring(uriString.LastIndexOf('/') + 1);
                        ImgurGalleryAsyncWrapper imgurGalleryAsyncWrapper = new ImgurGalleryAsyncWrapper();
                        imgurGalleryAsyncWrapper.NewLinksFromImgurGallery += UpdateImageLinksList_NewLinksFromImgurGallery;
                        imgurGalleryAsyncWrapper.FindImagesInGalleryAsync(ImgurClientID, uriString, pair.Key);
                    }
                    else if (LinkIsImgurImage(uriString))
                    {
                        WriteToDebugLog("New image detected! \n");
                        uriString = uriString.Substring(uriString.LastIndexOf('/') + 1);
                        ImgurImageAsyncWrapper imgurGalleryAsyncWrapper = new ImgurImageAsyncWrapper();
                        imgurGalleryAsyncWrapper.NewLinksFromImgurImage += UpdateImageLinksList_NewLinkFromImgurImage;
                        imgurGalleryAsyncWrapper.FindImageInLinkAsync(ImgurClientID, uriString, pair.Key);
                    }
                    else
                    {
                        WriteToDebugLog($"The link is in invalid format! - {pair.Key}\n");
                    }
                }
            }
        }
        private void WriteToDebugLog(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                    DebugLog.Text += $"{text}\n";
            });
        }
        #region Link Checking
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
        #endregion
        public void DownloadImageWithExtension(Uri url, ImageFormat format)
        {
            using (WebClient client = new WebClient())
            {
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
        private async void btn_SearchSubreddit_Click(object sender, RoutedEventArgs e)
        {
            if ((string)btn_CheckSubreddit.Content == "Exists" && listBox_FromTime.SelectedItem != null)
            {
                btn_SearchSubreddit.IsEnabled = false;
                textBlock_ErrorMessageForSearchBtn.Text = "Wait!";
                await GetTopLinksAsync(textB_SubredditInput.Text, fromtime, Convert.ToInt16(label_PostCount_Number.Content));
                textBlock_ErrorMessageForSearchBtn.Text = "Done!";
                btn_SearchSubreddit.IsEnabled = true;
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
        void UpdateImageLinksList_NewLinksFromImgurAlbum(List<IImage> iimageList, string albumName)
        {
            this.Dispatcher.Invoke(() =>
            {
                WriteToDebugLog("New Album Links Added\n");
                int count = iimageList.Count;
                List<Uri> uriList = new List<Uri>();
                for (int i = 0; i < count; i++)
                {
                    uriList.Add(new Uri(iimageList[i].Link));
                }
                imageLinkDict.Add(albumName, uriList);
            });
        }
        void UpdateImageLinksList_NewLinksFromImgurGallery(List<IImage> iimageList, string galleryName)
        {
            this.Dispatcher.Invoke(() =>
            {
                WriteToDebugLog("New Gallery Links Added\n");
                int count = iimageList.Count;
                List<Uri> uriList = new List<Uri>();
                for (int i = 0; i < count; i++)
                {
                    uriList.Add(new Uri(iimageList[i].Link));
                }
                imageLinkDict.Add(galleryName, uriList);
            });
        }
        void UpdateImageLinksList_NewLinkFromImgurImage(IImage iimage, string name)
        {
            this.Dispatcher.Invoke(() =>
            {
                WriteToDebugLog("New Image Link Added\n");
                imageLinkDict.Add(name, new List<Uri> { new Uri(iimage.Link) });
            });
        }
        #endregion
    }
    #region Wrappers
    public delegate void NewLinksFromImgurAlbumDelegate(List<IImage> iimageList, string albumName);
    public class ImgurAlbumAsyncWrapper
    {
        public List<IImage> iimage;
        string token;
        string albumlink;
        public event NewLinksFromImgurAlbumDelegate NewLinksFromImgurAlbum;
        public async void FindImagesInAlbumAsync(string token, string albumlink, string albumName)
        {
            this.token = token;
            this.albumlink = albumlink;
            iimage = await Task.Run(GetIenumerableIImagesAsync);
            NewLinksFromImgurAlbum(iimage, albumName);
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
    public delegate void NewLinksFromImgurGalleryDelegate(List<IImage> iimageList, string galleryName);
    public class ImgurGalleryAsyncWrapper
    {
        public List<IImage> iimage;
        public List<IImage> iimage2;
        string token;
        string gallerylink;
        public event NewLinksFromImgurGalleryDelegate NewLinksFromImgurGallery;
        public async void FindImagesInGalleryAsync(string token, string gallerylink, string galleryName)
        {
            //Using try and catch because sometimes the gallery doesn't have an album inside it. No idea how to get the Images then.
            try
            {
                this.token = token;
                this.gallerylink = gallerylink;
                iimage = await Task.Run(GetIenumerableIImagesAsync);
                NewLinksFromImgurGallery(iimage, galleryName);
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
    public delegate void NewLinkFromImgurImageDelegate(IImage iimage, string name);
    public class ImgurImageAsyncWrapper
    {
        string token;
        string imagelink;
        public event NewLinkFromImgurImageDelegate NewLinksFromImgurImage;
        public async void FindImageInLinkAsync(string token, string imagelink, string name)
        {
            this.token = token;
            this.imagelink = imagelink;
            NewLinksFromImgurImage(await Task.Run(AccessTheWebAsync), name);
        }
        async Task<IImage> AccessTheWebAsync()
        {
            var client = new ImgurClient(token);
            var endpoint = new ImageEndpoint(client);
            var images = await endpoint.GetImageAsync(imagelink);
            return images;
        }
    }
#endregion
}
