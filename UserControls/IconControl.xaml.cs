using ChangeFolderIcon.Models;
using ChangeFolderIcon.Utils.WindowsAPI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ChangeFolderIcon.UserControls
{
    public sealed partial class IconControl : UserControl
    {
        public IconControl() => InitializeComponent();

        #region ��������
        public IconInfo Icon
        {
            get => (IconInfo)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(IconInfo),
                typeof(IconControl), new PropertyMetadata(null));
        #endregion

        #region �϶��߼�

        // �ⲿ�ļ����ϵ���ͼ����ʱ����
        private void Root_DragOver(object sender, DragEventArgs e)
        {
            // ����϶��������Ƿ�����ļ�/�ļ���
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                // ���ò���Ϊ�����ơ�
                e.AcceptedOperation = DataPackageOperation.Copy;
                // �����϶�ʱ����ʾ����
                if (Icon != null)
                {
                    e.DragUIOverride.Caption = $"ʹ�� '{Icon.Name}' ͼ��";
                    e.DragUIOverride.IsCaptionVisible = true;
                }
            }
        }

        // �ⲿ�ļ����ڴ�ͼ���ϱ�����ʱ����
        private async void Root_Drop(object sender, DragEventArgs e)
        {
            if (Icon == null) return;
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            // ��ȡ���������Ŀ
            var items = await e.DataView.GetStorageItemsAsync();
            // ɸѡ�����е��ļ���
            var folders = items.OfType<StorageFolder>().ToList();
            if (folders.Count == 0) return;

            int ok = 0, fail = 0;
            // �������б�������ļ���
            foreach (var f in folders)
            {
                try
                {
                    // ʹ�ô˿ؼ������ͼ����Ӧ��
                    IconManager.SetFolderIcon(f.Path, Icon.FullPath);
                    ok++;
                }
                catch
                {
                    fail++;
                }
            }

            // ��ʾ�������
            await new ContentDialog
            {
                Title = "Ӧ�ý��",
                Content = $"�ɹ�Ӧ�õ� {ok} ���ļ��У�ʧ�� {fail} ����",
                CloseButtonText = "ȷ��",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }

        #endregion
    }
}
