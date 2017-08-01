using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Wokhan.WebControls
{
    [ToolboxData("<{0}:ExtendedDropDownList runat=server></{0}:ExtendedDropDownList>")]
    [SupportsEventValidation, ValidationProperty("SelectedItem")]
    public class ExtendedDropDownList : DropDownList
    {
        public class ListItemMixedCollection : List<object>
        {
            private ListItemCollection owner;

            internal ListItemMixedCollection(ListItemCollection src)
            {
                owner = src;
            }

            public new void Add(object item)
            {
                if (item is ListItem)
                {
                    base.Add(item);
                    owner.Add((ListItem)item);
                }
                else
                {
                    throw new ArgumentException("The Add method only accept ListItem objects.");
                }
            }

            public ListItemGroup AddGroup(string groupName)
            {
                var group = new ListItemGroup(owner) { Label = groupName };
                base.Add(group);
                return group;
            }

            public new void Insert(int index, object item)
            {
                if (item is ListItem)
                {
                    base.Insert(index, item);
                    owner.Insert(index, (ListItem)item);
                }
                else
                {
                    throw new ArgumentException("The Add method only accept ListItem objects.");
                }
            }

            public ListItemGroup InsertGroup(int index, string groupName)
            {
                var group = new ListItemGroup(owner) { Label = groupName };
                base.Add(group);
                return group;
            }
        }

        public class WrappedListItemCollection : List<ListItem>
        {
            ListItemCollection _lst;

            internal WrappedListItemCollection(ListItemCollection lst)
            {
                _lst = lst;
            }

            public void Add(ListItem item)
            {
                base.Add(item);
                _lst.Add(item);
            }

            public void Insert(int index, ListItem item)
            {
                base.Insert(index, item);
                _lst.Insert(index, item);
            }
        }

        public class ListItemGroup
        {
            public string Label { get; set; }
            public bool Enabled { get; set; }

            private System.Web.UI.AttributeCollection _attributes = new System.Web.UI.AttributeCollection(new StateBag(true));
            public System.Web.UI.AttributeCollection Attributes { get { return _attributes; } }

            private WrappedListItemCollection _items;
            public WrappedListItemCollection Items { get { return _items; } }

            public ListItemGroup(ListItemCollection src)
            {
                _items = new WrappedListItemCollection(src);
            }
        }

        private ListItemMixedCollection _items;

        public new ListItemMixedCollection Items { get { return _items; } }

        public ExtendedDropDownList()
        {
            _items = new ListItemMixedCollection(base.Items);
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            foreach (var g in Items)
            {
                if (g is ListItem)
                {
                    RenderItem((ListItem)g, writer);
                }
                else
                {
                    RenderItemGroup((ListItemGroup)g, writer);
                }
            }
        }

        private void RenderItemGroup(ListItemGroup g, HtmlTextWriter writer)
        {
            if (g.Label != null)
            {
                writer.WriteBeginTag("optgroup");
                writer.WriteAttribute("label", g.Label);
                writer.Write(HtmlTextWriter.TagRightChar);
            }

            foreach (ListItem li in g.Items)
            {
                RenderItem(li, writer);
            }

            if (g.Label != null)
            {
                writer.WriteEndTag("optgroup");
            }
        }

        private void RenderItem(ListItem li, HtmlTextWriter writer)
        {
            bool selected = false;

            writer.WriteBeginTag("option");
            if (li.Selected || base.Items.FindByValue(li.Value).Selected)
            {
                if (selected)
                {
                    VerifyMultiSelect();
                }
                selected = true;
                writer.WriteAttribute("selected", "selected");
            }

            if (!li.Enabled)
            {
                writer.WriteAttribute("disabled", "disabled");
            }

            writer.WriteAttribute("value", li.Value, true);

            // VSWhidbey 163920 Render expando attributes.
            if (li.Attributes != null && li.Attributes.Count > 0)
            {
                li.Attributes.Render(writer);
            }

            if (Page != null)
            {
                Page.ClientScript.RegisterForEventValidation(UniqueID, li.Value);
            }

            writer.Write(HtmlTextWriter.TagRightChar);
            HttpUtility.HtmlEncode(li.Text, writer);
            writer.WriteEndTag("option");
            writer.WriteLine();
        }

    }
}
