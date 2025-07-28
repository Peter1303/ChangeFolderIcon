using ChangeFolderIcon.Pages;
using ChangeFolderIcon.Utils.Events;
using ChangeFolderIcon.Utils.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChangeFolderIcon
{
    public sealed partial class MainWindow : Window
    {
        private readonly FolderNavigationService _folderService = new();
        private readonly IconsPage _iconsPage;

        public MainWindow()
        {
            InitializeComponent();

            _iconsPage = new IconsPage();
            _iconsPage.IconChanged += IconsPage_IconChanged;  // �����¼�
            ContentFrame.Content = _iconsPage;
            _iconsPage.UpdateState(null);
        }

        #region �� ѡ����ļ��� ���� ������
        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null) return;

            SelectFolderButton.IsEnabled = false;
            NavView.MenuItems.Clear();
            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = "������...",
                IsEnabled = false
            });

            try
            {
                List<FolderNavigationService.FolderNode> nodes =
                    await Task.Run(() => _folderService.BuildChildNodes(folder.Path));

                NavView.MenuItems.Clear();
                PopulateNavView(nodes, NavView.MenuItems);
            }
            catch (Exception ex)
            {
                NavView.MenuItems.Clear();
                NavView.MenuItems.Add(new NavigationViewItem
                {
                    Content = "����ʧ��: " + ex.Message,
                    IsEnabled = false
                });
            }
            finally { SelectFolderButton.IsEnabled = true; }
        }
        #endregion

        #region �� ���� NavigationView
        private void PopulateNavView(
            IEnumerable<FolderNavigationService.FolderNode> nodes, IList<object> menuItems)
        {
            foreach (var node in nodes)
            {
                var navItem = new NavigationViewItem { Content = node.Name, Tag = node.Path };
                SetNavItemIcon(navItem, node.IconPath);
                menuItems.Add(navItem);

                if (node.SubFolders.Any())
                    PopulateNavView(node.SubFolders, navItem.MenuItems);
            }
        }

        private static void SetNavItemIcon(NavigationViewItem item, string? iconPath)
        {
            var uri = !string.IsNullOrEmpty(iconPath) && File.Exists(iconPath)
                ? new Uri(iconPath)
                : new Uri("ms-appx:///Assets/icon/default.ico");

            item.Icon = new BitmapIcon { UriSource = uri, ShowAsMonochrome = false };
        }
        #endregion

        #region �� ���ѡ�� -> ֪ͨ IconsPage
        private void NavView_SelectionChanged(
            NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            string? path = (args.SelectedItemContainer as NavigationViewItem)?.Tag as string;
            _iconsPage.UpdateState(path);
        }
        #endregion

        #region �� IconsPage �ı�ͼ�� -> �ֲ�ˢ�� NavigationView
        private void IconsPage_IconChanged(object? sender, IconChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.FolderPath)) return;

            if (FindNavItemByPath(e.FolderPath, NavView.MenuItems) is NavigationViewItem navItem)
            {
                SetNavItemIcon(navItem, e.IconPath);
            }
        }

        private NavigationViewItem? FindNavItemByPath(
            string path, IList<object> items)
        {
            foreach (var obj in items)
            {
                if (obj is not NavigationViewItem nvi) continue;
                if (string.Equals(nvi.Tag as string, path, StringComparison.OrdinalIgnoreCase))
                    return nvi;

                if (nvi.MenuItems.Count > 0 &&
                    FindNavItemByPath(path, nvi.MenuItems) is NavigationViewItem found)
                    return found;
            }
            return null;
        }
        #endregion
    }
}
