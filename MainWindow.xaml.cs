using ChangeFolderIcon.Utils.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using static ChangeFolderIcon.Utils.Services.FolderNavigationService;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChangeFolderIcon
{
    public sealed partial class MainWindow : Window
    {
        private readonly FolderNavigationService _folderService = new FolderNavigationService();

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ��ѡ���ļ��С���ť�ĵ���¼�����
        /// </summary>
        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                SelectFolderButton.IsEnabled = false;
                NavView.MenuItems.Clear();
                var loadingItem = new NavigationViewItem { Content = "������...", IsEnabled = false };
                NavView.MenuItems.Add(loadingItem);

                try
                {
                    // 1. �ں�̨�߳��ϵ��÷�������������ģ����
                    List<FolderNode> childNodes = await Task.Run(() => _folderService.BuildChildNodes(folder.Path));

                    // 2. ����UI�̣߳���ռ���ָʾ��
                    NavView.MenuItems.Clear();

                    // 3. ���ݹ����õ�����ģ�ͣ���UI�߳������NavigationView
                    PopulateNavView(childNodes, NavView.MenuItems);
                }
                catch (Exception ex)
                {
                    NavView.MenuItems.Clear();
                    var errorItem = new NavigationViewItem { Content = $"����ʧ��: {ex.Message}", IsEnabled = false };
                    NavView.MenuItems.Add(errorItem);
                }
                finally
                {
                    SelectFolderButton.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// ʹ��Ԥ�ȹ����Ľڵ������ݹ���� NavigationView��
        /// </summary>
        /// <param name="nodes">Ҫ��ӵ�UI���ļ������ݽڵ��б�</param>
        /// <param name="menuItems">��������´����� NavigationViewItem ��UI����</param>
        private void PopulateNavView(List<FolderNode> nodes, IList<object> menuItems)
        {
            foreach (var node in nodes)
            {
                var navItem = new NavigationViewItem
                {
                    Content = node.Name,
                    Tag = node.Path,
                };

                // ����Ƿ�����Զ���ͼ��·��
                if (!string.IsNullOrEmpty(node.IconPath) && File.Exists(node.IconPath))
                {
                    try
                    {
                        // ������ڣ�ʹ�� BitmapIcon ��ʾ�Զ���ͼ��
                        navItem.Icon = new BitmapIcon
                        {
                            UriSource = new Uri(node.IconPath),
                            ShowAsMonochrome = false // ȷ����ʾ��ɫͼ��
                        };
                    }
                    catch (Exception)
                    {
                        // �������ͼ��ʧ�ܣ�����·����Ч��
                        navItem.Icon = new BitmapIcon
                        {
                            UriSource = new Uri("ms-appx:///Assets/icon/default.ico"),
                            ShowAsMonochrome = false
                        };
                    }
                }
                else
                {
                    // ����ʹ��Ĭ�ϵ��ļ���ͼ��
                    navItem.Icon = new BitmapIcon
                    {
                        UriSource = new Uri("ms-appx:///Assets/icon/default.ico"),
                        ShowAsMonochrome = false
                    };
                }

                menuItems.Add(navItem);

                // �ݹ�Ϊ�����ļ������˵���
                if (node.SubFolders.Any())
                {
                    PopulateNavView(node.SubFolders, navItem.MenuItems);
                }
            }
        }
    }
}