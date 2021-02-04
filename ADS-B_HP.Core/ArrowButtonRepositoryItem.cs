using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Drawing;
using DevExpress.XtraEditors.Registrator;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors.ViewInfo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.Core
{
    public class ArrowButton : EditorButton
    {
    }

    public class ArrowButtonViewInfo : ButtonEditViewInfo
    {
        public ArrowButtonViewInfo(RepositoryItem ri) : base(ri)
        {
        }
    }

    public class ArrowButtonPainter : ButtonEditPainter
    {
        protected override void DrawContent(ControlGraphicsInfoArgs info)
        {
            base.DrawContent(info);
        }
        protected override void DrawButton(ButtonEditViewInfo viewInfo, EditorButtonObjectInfoArgs info)
        {
            if (info.Button.Image == null) return;
            int cellH = info.Bounds.Height;
            int imageH = info.Button.Image.Size.Height;
            int y = (int)((cellH / 2) - (imageH / 2));
            Point imagePoint = info.Bounds.Location;
            imagePoint.X += 1;
            imagePoint.Y += y;
            info.Graphics.DrawImage(info.Button.Image, imagePoint);
        }
    }
    public class ArrowButtonRepositoryItem : RepositoryItemButtonEdit
    {
        public override void CreateDefaultButton() { }
        internal const string EditorName = "ArrowButton";

        public new RepositoryItemButtonEdit Properties
        {
            get { return this; }
        }
        public override void BeginInit()
        {
            base.BeginInit();
        }
        public override void EndInit()
        {
            base.EndInit();
        }
        static ArrowButtonRepositoryItem()
        {
            Register();
        }
        public static void Register()
        {
            EditorClassInfo editorClassInfo = new EditorClassInfo(EditorName, typeof(ArrowButton), typeof(ArrowButtonRepositoryItem), typeof(ArrowButtonViewInfo), new ArrowButtonPainter(), true, null);
            EditorRegistrationInfo.Default.Editors.Add(editorClassInfo);
        }
        public override string EditorTypeName
        {
            get { return EditorName; }
        }

    }
}
