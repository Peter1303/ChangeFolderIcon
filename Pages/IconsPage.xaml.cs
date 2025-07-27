using ChangeFolderIcon.Models;
using ChangeFolderIcon.Utils.WindowsAPI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChangeFolderIcon.Pages
{
    public sealed partial class IconsPage : Page
    {
        public ObservableCollection<IconInfo> Icons { get; } = new();

        private string? _selectedFolderPath;  // ���� MainWindow �ĵ�ǰѡ���ļ���
        private IconInfo? _selectedIcon;      // ��ǰ��ͼ�����ѡ�е�ͼ��

        public IconsPage()
        {
            this.InitializeComponent();
            LoadIconsFromAssets();
            UpdateHeaderAndActions();
        }

        private void LoadIconsFromAssets()
        {
            // Լ����Assets/ico λ�ڳ����Ŀ¼
            var baseDir = AppContext.BaseDirectory;
            var icoDir = Path.Combine(baseDir, "Assets", "ico");
            if (!Directory.Exists(icoDir)) return;

            foreach (var ico in Directory.EnumerateFiles(icoDir, "*.ico"))
            {
                try { Icons.Add(IconInfo.FromPath(ico)); }
                catch { /* ���Ի�ͼ�� */ }
            }
        }

        /// <summary> �� MainWindow ���ã����¡��Ƿ�ѡ���ļ��С���״̬�� </summary>
        public void UpdateState(string? selectedFolderPath)
        {
            _selectedFolderPath = selectedFolderPath;
            UpdateHeaderAndActions();
        }

        private void UpdateHeaderAndActions()
        {
            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                HeaderText.Text = "ѡ��һ��ϲ����ͼ�꣬��ק�ⲿ�ļ��е�ͼ�����Ӧ�á�";
                ActionPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                HeaderText.Text = $"��ѡ���ļ��У�{_selectedFolderPath}\nѡ��һ��ϲ����ͼ�겢Ӧ�á�";
                ActionPanel.Visibility = Visibility.Visible;
            }
        }

        private void IconsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            _selectedIcon = e.ClickedItem as IconInfo;
        }

        // ҳ�������ק֧�֣���ѡ��ǿ������ҳ��հ״�Ҳ��Ͷ���ļ���
        private void Page_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = _selectedIcon == null
                    ? "����ѡ��һ��ͼ��"
                    : "�ͷ��Խ���ǰͼ��Ӧ�õ�������ļ���";
            }
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (_selectedIcon == null)
            {
                var dlg = new ContentDialog
                {
                    Title = "��ʾ",
                    Content = "�������Ϸ�ͼ�����ѡ��һ��ͼ�ꡣ",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            var folders = items.OfType<StorageFolder>().ToList();
            if (folders.Count == 0) return;

            int ok = 0, fail = 0;
            foreach (var f in folders)
            {
                try { IconManager.SetFolderIcon(f.Path, _selectedIcon.FullPath); ok++; }
                catch { fail++; }
            }

            var resultDlg = new ContentDialog
            {
                Title = "Ӧ�ý��",
                Content = $"�ɹ���{ok}��ʧ�ܣ�{fail}",
                CloseButtonText = "ȷ��",
                XamlRoot = this.XamlRoot
            };
            await resultDlg.ShowAsync();
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIcon == null || string.IsNullOrEmpty(_selectedFolderPath))
            {
                var dlg = new ContentDialog
                {
                    Title = "��ʾ",
                    Content = "��ѡ��һ��ͼ�꣬������ർ����ѡ��Ŀ���ļ��С�",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
                return;
            }

            try
            {
                IconManager.SetFolderIcon(_selectedFolderPath!, _selectedIcon.FullPath);
                await new ContentDialog
                {
                    Title = "���",
                    Content = "��Ӧ�õ�ѡ���ļ��С�",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "ʧ��",
                    Content = ex.Message,
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        private async void ApplyAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIcon == null || string.IsNullOrEmpty(_selectedFolderPath))
            {
                var dlg = new ContentDialog
                {
                    Title = "��ʾ",
                    Content = "��ѡ��һ��ͼ�꣬������ർ����ѡ��Ŀ���ļ��С�",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
                return;
            }

            int count = IconManager.ApplyIconToAllSubfolders(_selectedFolderPath!, _selectedIcon.FullPath);

            await new ContentDialog
            {
                Title = "���",
                Content = $"��Ϊ {count} �����ļ���Ӧ��ͼ�ꡣ",
                CloseButtonText = "ȷ��",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                var dlg = new ContentDialog
                {
                    Title = "��ʾ",
                    Content = "������ർ����ѡ��һ��Ŀ���ļ��С�",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
                return;
            }
            try
            {
                IconManager.ClearFolderIcon(_selectedFolderPath!);
                await new ContentDialog
                {
                    Title = "���",
                    Content = $"��������ļ��е�ͼ�ꡣ",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "ʧ��",
                    Content = ex.Message,
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        private async void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                var dlg = new ContentDialog
                {
                    Title = "��ʾ",
                    Content = "������ർ����ѡ��һ��Ŀ���ļ��С�",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
                return;
            }
            try
            {
                int count = IconManager.ClearIconRecursively(_selectedFolderPath!);
                await new ContentDialog
                {
                    Title = "���",
                    Content = $"����� {count} �����ļ��е�ͼ�ꡣ",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "ʧ��",
                    Content = ex.Message,
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }
    }
}
