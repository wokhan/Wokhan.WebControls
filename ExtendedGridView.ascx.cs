using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Wokhan.WebControls
{
    [ToolboxData("<{0}:ExtendedGridView runat=server></{0}:ExtendedGridView>")]
    public class ExtendedGridView : CompositeControl
    {
        #region Fields

        private TableItemStyle _groupedCellStyle = new TableItemStyle() { CssClass = "ExtendedGridViewGroupCell" };
        private TableItemStyle _groupHeaderStyle = new TableItemStyle() { CssClass = "ExtendedGridViewGroupCellHeader" };
        private object _localSource = null;
        private Dictionary<string, string> _searchTerms = new Dictionary<string, string>();
        private TableItemStyle _sortedAscendingHeaderStyle = new TableItemStyle() { CssClass = "ExtendedGridViewHeaderCellSortedAscending" };
        private TableItemStyle _sortedDescendingHeaderStyle = new TableItemStyle() { CssClass = "ExtendedGridViewHeaderCellSortedDescending" };
        private List<int> boundaries = new List<int>();
        private Button btnClear;
        private Button btnSearch;
        private CheckBox chkTimer;
        private ExtendedDropDownList ddlFilterOn;
        private HtmlContainerControl divFilter;
        private HtmlContainerControl divMain;
        private HtmlContainerControl divTimer;
        private bool footerDone = false;
        private GridView gridEv;
        private List<int> groupableColsIndexes;
        private String groupId;
        private string groupVal = null;
        private HiddenField hidCtrlDown;
        private GridViewRow lastgroup = null;
        private Label lblCat;
        private Label lblFilterOn;
        private Panel pnlFilterOn;
        private int prevMin = -1;
        private bool requiresSort;
        private Repeater rptDetailedSearch;
        private Timer timeInfos;
        private UpdatePanel uplInfos;

        #endregion Fields

        /*protected override void OnInit(EventArgs e)
        {
            EnsureChildControls();

            base.OnInit(e);
        }*/

        #region Events (handlers / definitions)
        
        public event EventHandler FilterTextChanged;

        public event GridViewPageEventHandler PageIndexChanging;

        public event GridViewRowEventHandler RowDataBound;

        public event GridViewSortEventHandler Sorting;

        private void gridEv_DefaultRowDataBound(object sender, GridViewRowEventArgs e)
        {
            switch (e.Row.RowType)
            {
                case DataControlRowType.Header:
                    GenerateHeaderRow(e.Row);
                    if (ShowFilter)
                    {
                        GenerateFilter(e.Row);
                    }
                    break;

                case DataControlRowType.Pager:
                    if (ShowFooter)
                    {
                        GeneratePager(e.Row);
                    }
                    break;

                case DataControlRowType.Footer:
                    if (ShowFooter)
                    {
                        GenerateFooterRow(e.Row);
                        footerDone = true;
                    }
                    break;

                case DataControlRowType.DataRow:
                    GenerateRow(e.Row);
                    break;

                default:
                    break;
            }
        }

        private void gridEv_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            if (PageIndexChanging != null)
            {
                PageIndexChanging(sender, e);
            }

            if (!e.Cancel)
            {
                gridEv.PageIndex = e.NewPageIndex;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridEv_PreRender(object sender, EventArgs e)
        {
            if (gridEv.Rows.Count == 0)
            {
                gridEv.Enabled = false;
            }

            // Footer is not generated when there is no rows in the source, so we have to regenerate it anyway.
            if (ShowFooter && !footerDone)
            {
                GenerateFooterRow(null);
            }
        }

        /// <summary>
        /// Redispatches values in cells when datasource is an array-based enumerable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridEv_RowDataBoundForArray(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                for (int i = 0; i < e.Row.Cells.Count; i++)
                {
                    e.Row.Cells[i].Text = (((object[])e.Row.DataItem)[i] ?? String.Empty).ToString();
                }
            }
        }

        private void gridEv_Sorting(object sender, GridViewSortEventArgs e)
        {
            UpdateSortColumns(e);

            if (Sorting != null)
            {
                Sorting(sender, e);
            }

            if (SortColumns.Count > 0)
            {
                requiresSort = !e.Cancel;
            }
            else
            {
                requiresSort = false;
            }
        }

        private void rptDetailedSearch_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                var data = (KeyValuePair<string, string>)e.Item.DataItem;
                Label lblFilter = (Label)e.Item.FindControl("lblFilter");
                TextBox txtFilter = (TextBox)e.Item.FindControl("txtFilter");
                DropDownList ddlFilter = (DropDownList)e.Item.FindControl("ddlFilter");
                ITextControl tdfilter;

                Func<Dictionary<string, string>> sourceProvider;
                if (FilterSourcesProvider != null && FilterSourcesProvider.TryGetValue(data.Key, out sourceProvider))
                {
                    tdfilter = ddlFilter;

                    ddlFilter.DataSource = sourceProvider();
                    ddlFilter.DataValueField = "Key";
                    ddlFilter.DataTextField = "Value";
                    ddlFilter.ID = "txtFilter_" + data.Key;
                    ddlFilter.DataBind();

                    txtFilter.Visible = false;
                }
                else
                {
                    tdfilter = txtFilter;

                    txtFilter.ID = "txtFilter_" + data.Key;

                    ddlFilter.Visible = false;
                }

                lblFilter.ID = "lblFilter_" + data.Key;
                lblFilter.Text = data.Value;

                if (UseDetailedSearch)
                {
                    if (!String.IsNullOrEmpty(data.Key))
                    {
                        string sr;
                        tdfilter.Text = (SearchTerms.TryGetValue(data.Key, out sr) ? sr : null);
                    }
                }
                else if (SearchTerms.Count == 1)
                {
                    tdfilter.Text = SearchTerms.First().Value;
                }

                lblFilter.AssociatedControlID = txtFilter.ID;

                if (FilterTextChanged != null)
                {
                    ddlFilter.SelectedIndexChanged += FilterTextChanged;
                    txtFilter.TextChanged += FilterTextChanged;
                }

                var trig = new AsyncPostBackTrigger()
                {
                    ControlID = txtFilter.UniqueID,
                    EventName = "TextChanged"
                };
                uplInfos.Triggers.Add(trig);

                var trigD = new AsyncPostBackTrigger()
                {
                    ControlID = ddlFilter.UniqueID,
                    EventName = "SelectedIndexChanged"
                };
                uplInfos.Triggers.Add(trig);

                ScriptManager.GetCurrent(Page).RegisterAsyncPostBackControl(txtFilter);
                ScriptManager.GetCurrent(Page).RegisterAsyncPostBackControl(ddlFilter);

                if (DynamicSearch)
                {
                    //txtFilter.AutoPostBack = true;
                    ddlFilter.AutoPostBack = true;
                    txtFilter.Attributes.Add("onkeyup", "ExtendedGridView.autoFilter(this.value, '" + txtFilter.UniqueID + "');");
                }
            }
        }

        private void uplInfos_PreRender(object sender, EventArgs e)
        {
            ScriptManager.RegisterStartupScript(uplInfos, typeof(ExtendedGridView), gridEv.ClientID + "_load", "var " + gridEv.ClientID + "_scrollingTable = null; var " + gridEv.ClientID + "_scrollingTableFooter = null; ExtendedGridView.scrollHeaderAndFooter('" + gridEv.ClientID + "');", true);
        }

        #endregion Events

        #region Properties

        private string _internalId
        {
            get
            {
                return Page.Request.Url.GetHashCode() + "_" + this.UniqueID.GetHashCode();
                /*var ret = (string)ViewState["InternalGuid"];
                if (ret == null)
                {
                    ret = Guid.NewGuid().ToString();
                    ViewState["InternalGuid"] = ret;
                }
                return ret;*/
            }
        }

        private object localSource
        {
            get { return _localSource ?? (EnablePageSessionData ? Page.Session["ExtendedGridViewData_" + _internalId] : (EnableViewStateData ? ViewState["Data"] : null)); }
            set { _localSource = value; }
        }

        private List<KeyValuePair<string, string>> PostBackOptionsCache
        {
            get { return (List<KeyValuePair<string, string>>)ViewState["PostBackOptions"]; }
            set { ViewState["PostBackOptions"] = value; }
        }

        private string[] realGroupColumns
        {
            get { return ((string[])ViewState["realGroupColumns"] ?? GroupColumns); }
            set { ViewState["realGroupColumns"] = value; }
        }

        #region Innergrid properties

        public bool AllowCustomPaging
        {
            get { return gridEv.AllowCustomPaging; }
            set { gridEv.AllowCustomPaging = value; }
        }

        public bool AllowPaging
        {
            get { return gridEv.AllowPaging; }
            set { EnsureChildControls(); gridEv.AllowPaging = value; }
        }

        public bool AllowSorting
        {
            get { return gridEv.AllowSorting; }
            set { EnsureChildControls(); gridEv.AllowSorting = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableItemStyle AlternatingRowStyle
        {
            get { EnsureChildControls(); return gridEv.AlternatingRowStyle; }
        }

        public bool AutoGenerateColumns
        {
            get { return gridEv.AutoGenerateColumns; }
            set { EnsureChildControls(); gridEv.AutoGenerateColumns = value; }
        }

        [DefaultValue(4)]
        public int CellPadding
        {
            get { return gridEv.CellPadding; }
            set { EnsureChildControls(); gridEv.CellPadding = value; }
        }

        [DefaultValue(1)]
        public int CellSpacing
        {
            get { return gridEv.CellSpacing; }
            set { EnsureChildControls(); gridEv.CellSpacing = value; }
        }

        [Editor("System.Web.UI.Design.WebControls.DataControlFieldTypeEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        [DefaultValue("")]
        [MergableProperty(false)]
        public virtual DataControlFieldCollection Columns { get { EnsureChildControls(); return gridEv.Columns; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableItemStyle FooterStyle
        {
            get { EnsureChildControls(); return gridEv.FooterStyle; }
        }

        #endregion Innergrid properties

        public int ComputedOffsetBottom
        {
            get { return (int)(ViewState["ComputedOffsetBottom"] ?? 0); }
            set { ViewState["ComputedOffsetBottom"] = value; }
        }

        public int ComputedOffsetTop
        {
            get { return (int)(ViewState["ComputedOffsetTop"] ?? 0); }
            set { ViewState["ComputedOffsetTop"] = value; }
        }

        public Dictionary<string, Action<GridViewRow, TableCell>> CustomCellFormatters { get; set; }

        /// <summary>
        /// Defines the data source (which must be an <![CDATA[IEnumerable<T>]]>, with T being anything) for the grid.
        /// </summary>
        [Bindable(true)]
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Themeable(false)]
        public object DataSource
        {
            get { return localSource; }//?? (EnableViewStateData ? ViewState["Data"] : null); }
            set
            {
                localSource = value;
                if (EnablePageSessionData)
                {
                    Page.Session["ExtendedGridViewData_" + _internalId] = value;
                }
                else if (EnableViewStateData)
                {
                    ViewState["Data"] = value;
                }
            }
        }

        public bool DynamicSearch
        {
            get { return (bool)(ViewState["DynamicSearch"] ?? true); }
            set { ViewState["DynamicSearch"] = value; }
        }

        public bool EnablePageSessionData
        {
            get { return (bool)(ViewState["EnablePageSessionData"] ?? false); }
            set { ViewState["EnablePageSessionData"] = value; }
        }

        [Obsolete("EnableSessionData should be used instead to minimize Viewstate footprint with identical behavior")]
        public bool EnableViewStateData
        {
            get { return (bool)(ViewState["EnableViewStateData"] ?? false); }
            set { ViewState["EnableViewStateData"] = value; }
        }

        public bool FilterHandled { get; set; }

        public string FilterOnText
        {
            get { return (string)ViewState["FilterOnText"] ?? "Filter on:&nbsp;"; }
            set { ViewState["FilterOnText"] = value; }
        }

        public Dictionary<string, Func<Dictionary<string, string>>> FilterSourcesProvider
        {
            get;
            set;
        }

        public string FilterText
        {
            get { return (string)ViewState["FilterText"] ?? String.Empty; }
            set { ViewState["FilterText"] = value; }
        }

        public bool ForceSortWhenGroup
        {
            get { return (bool)(ViewState["ForceSortWhenGroup"] ?? true); }
            set { ViewState["ForceSortWhenGroup"] = value; }
        }

        public Dictionary<string, string> GridHeaders
        {
            get { return gridEv.HeaderRow.Cells.Cast<DataControlFieldCell>().ToDictionary(c => c.GetUnderlyingField(), c => ((BoundField)c.ContainingField).HeaderText); }
        }

        public Color[] GroupColors
        {
            get { return (Color[])ViewState["GroupColors"] ?? new[] { Color.LightGreen, Color.LightPink, Color.LightSalmon, Color.LightBlue, Color.LightCoral, Color.LightSeaGreen }; }
            set { ViewState["GroupColors"] = value; }
        }

        [TypeConverter(typeof(StringArrayConverter))]
        public string[] GroupColumns
        {
            get { return (string[])ViewState["GroupColumns"]; }
            set { ViewState["GroupColumns"] = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [NotifyParentProperty(true)]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        public TableItemStyle GroupedCellStyle
        {
            get { return _groupedCellStyle; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [NotifyParentProperty(true)]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        public TableItemStyle GroupHeaderCellStyle
        {
            get { return _groupHeaderStyle; }
        }

        public Dictionary<string, int> HeaderColumnsGroups
        {
            get { return (Dictionary<string, int>)ViewState["HeaderColumnsGroups"]; }
            set { ViewState["HeaderColumnsGroups"] = value; }
        }

        [TypeConverter(typeof(StringArrayConverter))]
        public string[] Headers
        {
            get { return (string[])ViewState["Headers"]; }
            set { ViewState["Headers"] = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableItemStyle HeaderStyle
        {
            get { EnsureChildControls(); return gridEv.HeaderStyle; }
        }

        public bool HideCellWhenGroup
        {
            get { return (bool)(ViewState["HideCellWhenGroup"] ?? true); }
            set { ViewState["HideCellWhenGroup"] = value; }
        }

        public bool HideGroupedColumnDef
        {
            get { return (bool)(ViewState["HideGroupedColumnDef"] ?? false); }
            set { ViewState["HideGroupedColumnDef"] = value; }
        }

        public string ItemsCountFormat
        {
            get { return (string)(ViewState["ItemsCountFormat"] ?? "{0} item(s) found ({1} page(s))"); }
            set { ViewState["ItemsCountFormat"] = value; }
        }

        public DateTime? LastUpdate
        {
            get { return (DateTime?)ViewState["LastUpdate"] ?? DateTime.Now; }
            set { ViewState["LastUpdate"] = value; }
        }

        public string LastUpdateFormat
        {
            get { return (string)ViewState["LastUpdateFormat"] ?? "Last update: {0:dd MMM yyyy HH:mm:ss}"; }
            set { ViewState["LastUpdateFormat"] = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public CssStyleCollection LastUpdateStyle
        {
            get { return (CssStyleCollection)ViewState["LastUpdateStyle"]; }
            set { ViewState["LastUpdateStyle"] = value; }
        }

        public int PageIndex
        {
            get { return gridEv.PageIndex; }
            set { gridEv.PageIndex = value; }
        }

        public int PageSize
        {
            get { return gridEv.PageSize; }
            set { EnsureChildControls(); gridEv.PageSize = value; }
        }

        public string RowHeaderColumn
        {
            get { return gridEv.RowHeaderColumn; }
            set { EnsureChildControls(); gridEv.RowHeaderColumn = value; }
        }

        [PersistenceMode(PersistenceMode.InnerProperty)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [NotifyParentProperty(true)]
        public TableItemStyle RowStyle
        {
            get { EnsureChildControls(); return gridEv.RowStyle; }
        }

        [TypeConverter(typeof(StringArrayConverter))]
        public string[] SearchColumns { get; set; }

        public Dictionary<string, string> SearchTerms
        {
            get
            {
                if (Page.IsPostBack || ScriptManager.GetCurrent(Page).IsInAsyncPostBack)
                {
                    if (UseDetailedSearch)
                    {
                        return Page.Request.Form.AllKeys.Where(k => k != null && k.StartsWith(rptDetailedSearch.UniqueID) && !String.IsNullOrEmpty(Page.Request.Form[k]))
                                                        .ToDictionary(f => f.Split(new[] { "txtFilter_" }, StringSplitOptions.RemoveEmptyEntries).Last(), f => Page.Request.Form[f]);
                    }
                    else if (!String.IsNullOrEmpty(Page.Request.Form[rptDetailedSearch.UniqueID + "$ctl00$txtFilter_"]))
                    {
                        return new Dictionary<string, string> { { ddlFilterOn.SelectedValue, Page.Request.Form[rptDetailedSearch.UniqueID + "$ctl00$txtFilter_"] } };
                    }
                    else
                    {
                        return new Dictionary<string, string>();
                    }
                }
                else
                {
                    return _searchTerms;
                }
            }
            set { _searchTerms = value; }
        }

        public bool ShowFilter
        {
            get { return (bool)(ViewState["ShowFilter"] ?? true); }
            set { ViewState["ShowFilter"] = value; }
        }

        public bool ShowFooter
        {
            get { return gridEv.ShowFooter; }
            set { EnsureChildControls(); gridEv.ShowFooter = value; }
        }

        public bool ShowItemsCount
        {
            get { return (bool)(ViewState["ShowItemsCount"] ?? true); }
            set { ViewState["ShowItemsCount"] = value; }
        }

        public bool ShowLastUpdate
        {
            get { return (bool)(ViewState["ShowLastUpdate"] ?? true); }
            set { ViewState["ShowLastUpdate"] = value; }
        }

        public Dictionary<string, SortDirection> SortColumns
        {
            get { return (Dictionary<string, SortDirection>)ViewState["SortColumns"]; }
            set { ViewState["SortColumns"] = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [NotifyParentProperty(true)]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        public TableItemStyle SortedAscendingHeaderStyle
        {
            get { return _sortedAscendingHeaderStyle; }
        }

        [NotifyParentProperty(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        public TableItemStyle SortedDescendingHeaderStyle
        {
            get { return _sortedDescendingHeaderStyle; }
        }

        public bool TimerTicked
        {
            get { return ScriptManager.GetCurrent(Page).AsyncPostBackSourceElementID == timeInfos.UniqueID; }
        }

        public string Title { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public CssStyleCollection TitleStyle
        {
            get { return lblCat.Style; }
        }

        public string UpdateCheckboxFormat
        {
            get { return (string)ViewState["UpdateCheckboxFormat"] ?? "Enable automatic update ({0}ms interval)"; }
            set { ViewState["UpdateCheckboxFormat"] = value; }
        }

        public int UpdateInterval
        {
            get { return (int)(ViewState["UpdateInterval"] ?? -1); }
            set { ViewState["UpdateInterval"] = value; }
        }

        public bool UseDetailedSearch
        {
            get { return (bool)(ViewState["UseDetailedSearch"] ?? false); }
            set { ViewState["UseDetailedSearch"] = value; }
        }

        public int VirtualItemCount
        {
            get { return gridEv.VirtualItemCount; }
            set { gridEv.VirtualItemCount = value; }
        }

        #endregion Properties

        #region Methods

        private void ApplySort()
        {
            // Sorting by specified columns
            if (requiresSort)
            {
                localSource = localSource.ApplySort(SortColumns);
            }
            // or by default ones
            else if (realGroupColumns != null && realGroupColumns.Length > 0)
            {
                if (SortColumns != null)
                {
                    var ss = realGroupColumns.Where(g => !SortColumns.ContainsKey(g)).ToDictionary(g => g, g => SortDirection.Ascending);
                    SortColumns = ss.Concat(SortColumns).ToDictionary(gs => gs.Key, gs => gs.Value);
                }
                else
                {
                    SortColumns = realGroupColumns.ToDictionary(g => g, g => SortDirection.Ascending);
                }

                localSource = localSource.ApplySort(SortColumns);
            }
        }

        #region Generation

        private void GenerateColumns()
        {
            if (localSource is IEnumerable && ((IEnumerable)localSource).IsArrayEnumerable())
            {
                if (Headers == null)
                {
                    Headers = ((IEnumerable)localSource).Cast<object[]>().First().Select(h => h.ToString()).ToArray();
                    localSource = ((IEnumerable)localSource).Cast<object[]>().Skip(1);
                }
                else
                {
                    localSource = ((IEnumerable)localSource).Cast<object[]>();
                }

                gridEv.AutoGenerateColumns = false;
                for (int i = 0; i < Headers.Length; i++)
                {
                    gridEv.Columns.Add(new BoundField() { DataField = "!", HeaderText = Headers[i], SortExpression = i.ToString(), HtmlEncode = false });
                }

                if (GroupColumns != null)
                {
                    realGroupColumns = GroupColumns.Select(g => Headers.IndexOf(g)).Where(i => i != -1).Select(i => i.ToString()).ToArray();
                }

                //localSource = ((IEnumerable)localSource).Cast<object[]>().ToArray();

                //gridEv.RowDataBound += new GridViewRowEventHandler(gridEv_RowDataBoundForArray);
            }
            else if (Headers != null)
            {
                gridEv.AutoGenerateColumns = false;
                for (int i = 0; i < Headers.Length; i++)
                {
                    string header = Headers[i];
                    gridEv.Columns.Add(new BoundField() { DataField = header, HeaderText = header, SortExpression = header, HtmlEncode = false });
                }

                if (GroupColumns != null)
                {
                    realGroupColumns = GroupColumns.Intersect(Headers).ToArray();
                }
            }
            else
            {
                realGroupColumns = GroupColumns;
            }
        }

        private void GenerateFilter(GridViewRow headerRow)
        {
            var realHeads = headerRow.Cells.Cast<DataControlFieldCell>()
                                           .ToDictionary(c => c.GetUnderlyingField(), c => c.HasControls() ? ((LinkButton)c.Controls[0]).Text : c.Text);

            if (UseDetailedSearch)
            {
                pnlFilterOn.Visible = false;
                rptDetailedSearch.DataSource = SearchColumns != null ? SearchColumns.ToDictionary(sc => sc, sc => realHeads[sc]) : realHeads;
            }
            else
            {
                lblFilterOn.Text = FilterOnText;

                ddlFilterOn.Items.Clear();
                ddlFilterOn.Items.Add(new ListItem("< Any >", ""));

                var ix = 0;
                var hcols = HeaderColumnsGroups != null ? HeaderColumnsGroups.ToDictionary(x =>
                {
                    var ret = ix;
                    ix += x.Value;
                    return ret;
                }
                    , x => x.Key) : null;

                ExtendedDropDownList.ListItemGroup gx = null;
                var i = 0;
                foreach (var realHead in realHeads)
                {
                    if (hcols != null && hcols.ContainsKey(i))
                    {
                        gx = ddlFilterOn.Items.AddGroup(hcols[i]);
                    }

                    if (gx != null)
                    {
                        gx.Items.Add(new ListItem(realHead.Value, realHead.Key));
                    }
                    else
                    {
                        ddlFilterOn.Items.Add(new ListItem(realHead.Value, realHead.Key));
                    }
                    i++;
                }

                if (SearchTerms.Keys.Count == 1)
                {
                    ddlFilterOn.SelectedValue = SearchTerms.First().Key;
                }

                rptDetailedSearch.DataSource = new Dictionary<string, string>() { { "", "" } };
            }

            if (DynamicSearch)
            {
                btnSearch.Visible = false;
            }

            rptDetailedSearch.ItemDataBound += rptDetailedSearch_ItemDataBound;
            rptDetailedSearch.DataBind();

            /*ddlFilterOn.DataSource = new[] { new KeyValuePair<string, string>("", "< Any >") }
                .Concat(gridEv.HeaderRow.Cells.Cast<DataControlFieldCell>()
                .Select((c, i) => new KeyValuePair<string, string>(c.ContainingField.SortExpression, (HeaderColumnsGroups != null ? HeaderColumnsGroups.FirstOrDefault(h => h.Value >= i).Key : "") + (c.HasControls() ? ((LinkButton)c.Controls[0]).Text : c.Text))));
            ddlFilterOn.DataTextField = "value";
            ddlFilterOn.DataValueField = "key";

            ddlFilterOn.DataBind();*/
        }

        private void GenerateFooterRow(GridViewRow footerRow)
        {
            int nbcells = 1;
            if (footerRow == null)
            {
                footerRow = new GridViewRow(-1, -1, DataControlRowType.Footer, DataControlRowState.Normal);
                gridEv.Controls[0].Controls.Add(footerRow);
                if (gridEv.HeaderRow != null)
                {
                    nbcells = gridEv.HeaderRow.Cells.Count;
                }
            }
            else
            {
                nbcells = footerRow.Cells.Count;
            }
            footerRow.Cells.Clear();
            footerRow.Cells.Add(new TableCell() { ColumnSpan = nbcells });

            footerRow.ID = "firstFooterRow";
            footerRow.TableSection = TableRowSection.TableFooter;

            if (ShowLastUpdate)
            {
                footerRow.Cells[footerRow.Cells.Count - 1].Controls.Add(new Label()
                {
                    CssClass = "ExtendedGridViewLastUpdate",
                    Text = String.Format(DateTimeFormatInfo.InvariantInfo, LastUpdateFormat, LastUpdate)
                });
            }

            if (ShowItemsCount)
            {
                var cntres = gridEv.AllowCustomPaging ? gridEv.VirtualItemCount :
                             localSource is DataTable ? ((DataTable)localSource).DefaultView.Count :
                             localSource is DataView ? ((DataView)localSource).Count :
                             ((IEnumerable)localSource).Cast<object>().Count();
                footerRow.Cells[footerRow.Cells.Count - 1].Controls.Add(new Label()
                {
                    CssClass = "ExtendedGridViewItemsCount",
                    Text = String.Format(ItemsCountFormat, cntres, AllowPaging ? Math.Ceiling(cntres / (double)PageSize) : 1)
                });
            }
        }

        /// <summary>
        /// Handles the header row creation
        /// </summary>
        /// <param name="headerRow"></param>
        private void GenerateHeaderRow(GridViewRow headerRow)
        {
            headerRow.TableSection = TableRowSection.TableHeader;

            if (AllowSorting)
            {
                gridEv.HeaderStyle.CssClass += " ExtendedGridViewHeaderSortable";
            }

            // Fix for the table layout error (when using PagerMode = TopAndBottom)
            if (gridEv.TopPagerRow != null)
            {
                gridEv.Controls[0].Controls.Remove(headerRow);
                gridEv.Controls[0].Controls.AddAt(0, headerRow);
                gridEv.TopPagerRow.TableSection = TableRowSection.TableHeader;
                gridEv.Controls[0].Controls.Remove(gridEv.TopPagerRow);
                gridEv.Controls[0].Controls.AddAt(gridEv.Controls[0].Controls.IndexOf(headerRow), gridEv.TopPagerRow);
            }
            headerRow.ID = "firstHeaderRow";

            if (HeaderColumnsGroups != null && HeaderColumnsGroups.Any())
            {
                var headerGroupsRow = new GridViewRow(0, 0, DataControlRowType.Header, DataControlRowState.Normal);
                headerGroupsRow.Cells.AddRange(HeaderColumnsGroups.Select(x => new TableHeaderCell() { Text = x.Key, ColumnSpan = x.Value }).ToArray());
                headerGroupsRow.TableSection = TableRowSection.TableHeader;
                headerGroupsRow.CssClass = "ExtendedGridViewHeaderGroupsRow";
                gridEv.Controls[0].Controls.AddAt(0, headerGroupsRow);
            }

            if (localSource is DataTable || localSource is DataView)
            {
                DataTable dtSrc = (localSource is DataTable ? (DataTable)localSource : ((DataView)localSource).Table);
                foreach (DataControlFieldCell headerCell in headerRow.Cells)
                {
                    var refColumn = dtSrc.Columns[headerCell.ContainingField.HeaderText];
                    if (refColumn != null && !String.IsNullOrEmpty(refColumn.Caption))
                    {
                        if (headerCell.HasControls())
                        {
                            ((LinkButton)headerCell.Controls[0]).Text = refColumn.Caption;
                        }
                        else
                        {
                            headerCell.Text = refColumn.Caption;
                        }
                    }
                }
            }

            if (AllowSorting && SortColumns != null)
            {
                int sortCpt = 1;
                /*var srcCol = SortColumns.AsEnumerable();
                if (HideCellWhenGroup && GroupColumns != null)
                {
                    srcCol = srcCol.Where(s => !GroupColumns.Contains(s.Key));
                }*/
                foreach (KeyValuePair<string, SortDirection> colSort in SortColumns)
                {
                    var cell = headerRow.Cells.OfType<DataControlFieldCell>().First(c => (String.IsNullOrEmpty(c.ContainingField.SortExpression) ? c.GetUnderlyingField() : c.ContainingField.SortExpression) == colSort.Key);
                    if (SortColumns.Count > 1)
                    {
                        cell.Controls.Add(new Label() { Text = sortCpt.ToString(), CssClass = "ExtendedGridViewSortCounter" });
                        sortCpt++;
                    }
                    cell.ApplyStyle(colSort.Value == SortDirection.Ascending ? SortedAscendingHeaderStyle : SortedDescendingHeaderStyle);
                }
            }

            PostBackOptionsCache = new List<KeyValuePair<string, string>>();
            foreach (TableCell c in headerRow.Cells)
            {
                if (c.HasControls())
                {
                    LinkButton lnk = (LinkButton)c.Controls[0];
                    PostBackOptionsCache.Add(new KeyValuePair<string, string>(gridEv.UniqueID, lnk.CommandName + "$" + lnk.CommandArgument));
                    lnk.OnClientClick = "return false;";
                    c.Attributes["onclick"] = Page.ClientScript.GetPostBackEventReference(gridEv, lnk.CommandName + "$" + lnk.CommandArgument) + "; return false;";
                }
            }

            if (GroupColumns != null)
            {
                groupableColsIndexes = headerRow.Cells.Cast<DataControlFieldCell>()
                                      .Select((c, i) => new { Index = i, Cell = c })
                                      .Join(GroupColumns, c => c.Cell.GetUnderlyingField(), gc => gc, (c, gc) => c.Index)
                                      .ToList();

                for (int groupCnt = 0; groupCnt < groupableColsIndexes.Count; groupCnt++)
                {
                    var i = groupableColsIndexes[groupCnt];
                    var headerCell = headerRow.Cells[i];

                    if (HideCellWhenGroup)
                    {
                        if (headerCell.HasControls())
                        {
                            ((LinkButton)headerCell.Controls[0]).Text = String.Empty;
                        }
                        else
                        {
                            headerCell.Text = String.Empty;
                        }
                    }

                    headerCell.CssClass += " ExtendedGridViewHeaderCellGroup";
                    headerCell.BackColor = GroupColors[groupCnt % GroupColors.Length];//) + " !important";
                }
            }
        }

        private void GeneratePager(GridViewRow pagerRow)
        {
            Table pagerTable = (Table)pagerRow.Controls[0].Controls[0];
            pagerTable.CellPadding = 0;
            pagerTable.CellSpacing = 0;
            pagerTable.CssClass = "ExtendedGridViewPager";

            pagerRow.Parent.Controls.Remove(pagerRow);

            gridEv.FooterRow.Cells[0].Controls.AddAt(0, pagerTable);
        }

        private void GenerateRow(GridViewRow row)
        {
            prevMin++;

            DataControlFieldCell cell;
            string field;
            string value = null;
            Action<GridViewRow, TableCell> formatter;
            for (int i = 0; i < row.Cells.Count; i++)
            {
                cell = (DataControlFieldCell)row.Cells[i];
                field = cell.GetUnderlyingField();

                // Highlight the searched terms (if any)
                // Should optimize to avoid checking everytime

                if ((SearchTerms.Count == 1 && SearchTerms.First().Key == "") || SearchTerms.TryGetValue(field, out value))
                //if (SearchTerms.ContainsKey(field) || (SearchTerms.Count == 1 && SearchTerms.First().Key == ""))
                {
                    value = value ?? SearchTerms.First().Value;
                    cell.Text = Regex.Replace(cell.Text, Regex.Escape(value), (m) => "<span class='ExtendedGridViewHighlightedText'>" + m.Groups[0].Value + "</span>", RegexOptions.IgnoreCase);
                }

                if (CustomCellFormatters != null && CustomCellFormatters.TryGetValue(field, out formatter))
                {
                    formatter(row, cell);
                }
            }

            // Grouping
            if (GroupColumns != null && GroupColumns.Any())
            {
                Color groupColor;
                for (int groupCnt = 0; groupCnt < groupableColsIndexes.Count; groupCnt++)
                {
                    var i = groupableColsIndexes[groupCnt];
                    groupColor = GroupColors[groupCnt % GroupColors.Length];

                    cell = (DataControlFieldCell)row.Cells[i];
                    // New group => create a new grouping row
                    if (cell.Text != groupVal || boundaries.Contains(prevMin))
                    {
                        //Todo: remove
                        boundaries.Add(prevMin);

                        groupVal = cell.Text;
                        groupId = Guid.NewGuid().ToString();

                        var gvr = new GridViewRow(-1, -1, DataControlRowType.DataRow, DataControlRowState.Normal);
                        gvr.ClientIDMode = ClientIDMode.Static;
                        gvr.ID = groupId;
                        gvr.Attributes["data-exgrouphead"] = "true";
                        if (groupCnt > 1 && HideGroupedColumnDef)
                        {
                            gvr.Style["display"] = "none";
                            gvr.Attributes["data-exgroup"] = gridEv.Rows[prevMin].Attributes["data-exgroup"];
                        }
                        for (int colindex = 0; colindex < i; colindex++)
                        {
                            var cc = new TableCell();
                            if (lastgroup != null && lastgroup.Cells.Count > colindex)
                            {
                                cc.BackColor = lastgroup.Cells[colindex].BackColor;
                                cc.MergeStyle(GroupedCellStyle);
                            }
                            gvr.Cells.Add(cc);
                        }

                        var groupedCell = new TableHeaderCell() { ColumnSpan = gridEv.HeaderRow.Cells.Cast<TableCell>().Where(c => c.Visible).Count() - i, BackColor = groupColor };
                        groupedCell.MergeStyle(GroupHeaderCellStyle);
                        groupedCell.Attributes["onclick"] = "ExtendedGridView.toggleGroupedRows('" + groupId + "');";
                        gvr.Cells.Add(groupedCell);

                        var divgroup = (HtmlContainerControl)new HtmlGenericControl("div");
                        groupedCell.Controls.Add(divgroup);

                        var grouptitle = new HtmlGenericControl("span") { InnerHtml = ((DataControlFieldCell)gridEv.HeaderRow.Cells[i]).ContainingField.HeaderText + ": " };
                        grouptitle.Attributes["class"] = "ExtendedGridViewGroupTitle";
                        divgroup.Controls.Add(grouptitle);

                        var groupvalue = new HtmlGenericControl("span") { InnerHtml = cell.Text };
                        groupvalue.Attributes["class"] = "ExtendedGridViewGroupValue";
                        divgroup.Controls.Add(groupvalue);

                        row.Parent.Controls.AddAt(prevMin + (HeaderColumnsGroups != null && HeaderColumnsGroups.Any() ? 1 : 0) + boundaries.Count(b => b <= prevMin), gvr);//prevMin + 1 + offset, gvr);

                        lastgroup = gvr;
                    }

                    if (HideCellWhenGroup)
                    {
                        cell.Text = String.Empty;// "g" + boundaries.Count;
                    }

                    if (HideGroupedColumnDef)
                    {
                        row.Style["display"] = "none";
                        row.Attributes["data-exgroup"] = groupId;
                    }

                    cell.BackColor = groupColor;
                    cell.MergeStyle(GroupedCellStyle);
                }
            }
        }

        #endregion Generation

        #region Construction

        protected override void CreateChildControls()
        {
            Controls.Clear();
            CreateControlHierarchy();
            //ClearChildViewState();
        }

        protected virtual void CreateControlHierarchy()
        {
            Control control;
            using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Wokhan.WebControls.ExtendedGridView.ascx")))
            {
                control = ((Page)HttpContext.Current.Handler).ParseControl(sr.ReadToEnd());
            }

            this.Controls.Add(control);

            divMain = (HtmlContainerControl)control.FindControl("divMain");
            gridEv = (GridView)control.FindControl("gridEv");
            lblCat = (Label)control.FindControl("lblCat");
            chkTimer = (CheckBox)control.FindControl("chkTimer");
            timeInfos = (Timer)control.FindControl("timeInfos");
            uplInfos = (UpdatePanel)control.FindControl("uplInfos");
            divFilter = (HtmlContainerControl)control.FindControl("divFilter");
            lblFilterOn = (Label)control.FindControl("lblFilterOn");
            ddlFilterOn = (ExtendedDropDownList)control.FindControl("ddlFilterOn");
            hidCtrlDown = (HiddenField)control.FindControl("hidCtrlDown");
            divTimer = (HtmlContainerControl)control.FindControl("divTimer");
            rptDetailedSearch = (Repeater)control.FindControl("rptDetailedSearch");
            btnSearch = (Button)control.FindControl("btnSearch");
            btnClear = (Button)control.FindControl("btnClear");
            pnlFilterOn = (Panel)control.FindControl("pnlFilterOn");

            gridEv.PageIndexChanging += gridEv_PageIndexChanging;
            gridEv.Sorting += gridEv_Sorting;

            uplInfos.PreRender += new EventHandler(uplInfos_PreRender);
        }

        #endregion

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ScriptManager.RegisterClientScriptResource(this, typeof(ExtendedGridView), "Wokhan.WebControls.Scripts.ExtendedGridView.js");
            this.RegisterCSS("Wokhan.WebControls.Styles.ExtendedGridView.css");
        }

        #endregion Embedded ASCX management & constructor

        #region Properties
        /*public bool CausedPostback
        {
            get
            {
                var sc = ScriptManager.GetCurrent(Page);
                return !sc.IsInAsyncPostBack ||
                       sc.AsyncPostBackSourceElementID == gridEv.UniqueID ||
                       sc.AsyncPostBackSourceElementID == timeInfos.UniqueID ||
                       sc.AsyncPostBackSourceElementID == chkTimer.UniqueID ||
                       sc.AsyncPostBackSourceElementID.StartsWith(rptDetailedSearch.UniqueID) ||
                       sc.AsyncPostBackSourceElementID == ddlFilterOn.UniqueID ||
                       sc.AsyncPostBackSourceElementID == btnSearch.UniqueID ||
                       sc.AsyncPostBackSourceElementID == btnClear.UniqueID;
            }
        }*/
        /*       public string[] values
               {
                   get
                   {
                       return Page.Request.Form.AllKeys.Where(k => k.StartsWith(rptDetailedSearch.UniqueID) && !String.IsNullOrEmpty(Page.Request.Form[k]))
                                                        .Select(f => Page.Request.Form[f]).ToArray();
                   }
               }
       */
        /*public bool MoveGroupedColumnsToFront
        {
            get { return (bool)(ViewState["MoveGroupedColumnsToFront"] ?? false); }
            set { ViewState["MoveGroupedColumnsToFront"] = value; }
        }*/

        #endregion Properties
        
        #region Rendering methods
        protected override void OnPreRender(EventArgs e)
        {
            timeInfos.Enabled = chkTimer.Checked;

            if (localSource == null)// || (ScriptManager.GetCurrent(Page).IsInAsyncPostBack && !CausedPostback))
            {
                uplInfos.Update();
                return;
            }

            GenerateColumns();

            if (!FilterHandled)
            {
                localSource.ApplyFilter(SearchTerms);
            }

            ApplySort();

            gridEv.RowDataBound += gridEv_DefaultRowDataBound;
            if (RowDataBound != null)
            {
                gridEv.RowDataBound += RowDataBound;
            }

            if (String.IsNullOrEmpty(Title))
            {
                lblCat.Visible = false;
            }
            else
            {
                lblCat.Text = Title;
            }

            divFilter.Visible = ShowFilter;
            if (UpdateInterval > 0)
            {
                chkTimer.Text = String.Format(UpdateCheckboxFormat, UpdateInterval);
            }
            else
            {
                divTimer.Visible = false;
            }

            if (chkTimer.Visible)
            {
                timeInfos.Interval = UpdateInterval;
            }
            else
            {
                timeInfos.Enabled = false;
            }

            gridEv.PreRender += gridEv_PreRender;

            gridEv.DataSource = localSource;
            gridEv.DataBind();

            uplInfos.Update();

            base.OnPreRender(e);
        }

        protected override void Render(HtmlTextWriter writer)
        {
            ScriptManager.RegisterStartupScript(uplInfos, typeof(ExtendedGridView), gridEv.ClientID + "_docClick", "$addHandler(document.documentElement, 'click', function(e) { ExtendedGridView.scrollHeaderAndFooter('" + gridEv.ClientID + "'); });", true);
            ScriptManager.RegisterStartupScript(uplInfos, typeof(ExtendedGridView), gridEv.ClientID + "_CTRLDown", "$addHandler(document.body, 'keydown', function(e) { if (e.keyCode == 17) document.getElementById('" + hidCtrlDown.ClientID + "').value = 'true'; });", true);
            ScriptManager.RegisterStartupScript(uplInfos, typeof(ExtendedGridView), gridEv.ClientID + "_CTRLUp", "$addHandler(document.body, 'keyup', function(e) { if (e.keyCode == 17) document.getElementById('" + hidCtrlDown.ClientID + "').value = 'false'; });", true);
            ScriptManager.RegisterStartupScript(uplInfos, typeof(ExtendedGridView), gridEv.ClientID + "_scroll", "$addHandler(window.top, 'scroll', function() { ExtendedGridView.scrollHeaderAndFooter('" + gridEv.ClientID + "'); });", true);
            ScriptManager.RegisterStartupScript(uplInfos, typeof(ExtendedGridView), gridEv.ClientID + "_scrollresize", "$addHandler(window, 'resize', function() { ExtendedGridView.scrollHeaderAndFooter('" + gridEv.ClientID + "', true); });", true);
            ScriptManager.RegisterStartupScript(uplInfos, typeof(ExtendedGridView), gridEv.ClientID + "_compTopOffset", "window['" + gridEv.ClientID + "_compTopOffset'] = " + this.ComputedOffsetTop + ";", true);
            ScriptManager.RegisterStartupScript(uplInfos, typeof(ExtendedGridView), gridEv.ClientID + "_compBottomOffset", "window['" + gridEv.ClientID + "_compBottomOffset'] = " + this.ComputedOffsetBottom + ";", true);

            base.Render(writer);
        }

        public void RegisterHeaderClientEvents()
        {
            if (PostBackOptionsCache != null)
            {
                foreach (KeyValuePair<string, string> pbo in PostBackOptionsCache)
                {
                    Page.ClientScript.RegisterForEventValidation(pbo.Key, pbo.Value);
                }
            }
        }
        #endregion Rendering methods

        public void UpdateSortColumns(GridViewSortEventArgs e)
        {
            gridEv.PageIndex = 0;

            if (SortColumns == null)
            {
                SortColumns = new Dictionary<string, SortDirection>();
            }

            if (SortColumns.Count > 0 && hidCtrlDown.Value != "true")
            {
                SortColumns = SortColumns.Where(s => s.Key == e.SortExpression || (realGroupColumns != null && realGroupColumns.Contains(s.Key))).ToDictionary(s => s.Key, s => s.Value);
            }

            if (SortColumns.ContainsKey(e.SortExpression))
            {
                if (SortColumns[e.SortExpression] == SortDirection.Descending && (realGroupColumns == null || !realGroupColumns.Contains(e.SortExpression)))
                {
                    SortColumns.Remove(e.SortExpression);
                }
                else
                {
                    SortColumns[e.SortExpression] = (SortColumns[e.SortExpression] == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending);
                }
            }
            else
            {
                SortColumns.Add(e.SortExpression, e.SortDirection);
            }

            // To avoir the ctrl key being not detected as released after partial postbacks.
            hidCtrlDown.Value = "false";
        }
        
        #region Legacy group rows

        /*private void GroupRows()
        {
            if (GroupColumns != null)
            {
                GridViewRow lastgroup = null;

                var groupableColsIndexes = gridEv.HeaderRow.Cells.Cast<DataControlFieldCell>()
                                                                 .Select((c, i) => new { Index = i, Cell = c })
                                                                 .Join(GroupColumns, c => c.Cell.GetUnderlyingField(), gc => gc, (c, gc) => c.Index)
                                                                 .ToList();

                List<int> boundaries = new List<int>();
                Color groupColor;
                for (int groupCnt = 0; groupCnt < groupableColsIndexes.Count; groupCnt++)
                {
                    var i = groupableColsIndexes[groupCnt];
                    groupColor = GroupColors[groupCnt % GroupColors.Length];

                    if (HideCellWhenGroup)
                    {
                        var headerCell = gridEv.HeaderRow.Cells[i];
                        if (headerCell.HasControls())
                        {
                            ((LinkButton)headerCell.Controls[0]).Text = String.Empty;
                        }
                        else
                        {
                            headerCell.Text = String.Empty;
                        }

                        headerCell.CssClass += " ExtendedGridViewHeaderCellGroup";
                        headerCell.BackColor = groupColor;//) + " !important";
                    }

                    int prevMin = -1;
                    string groupVal = null;
                    GridViewRow r;
                    TableCell cell;
                    string groupId = null;
                    while (++prevMin < gridEv.Rows.Count)
                    {
                        r = gridEv.Rows[prevMin];
                        cell = r.Cells[i];
                        // New group => create a new grouping row
                        if (cell.Text != groupVal || boundaries.Contains(prevMin))
                        {
                            //Todo: remove
                            boundaries.Add(prevMin);

                            groupVal = cell.Text;
                            groupId = Guid.NewGuid().ToString();

                            var gvr = new GridViewRow(-1, -1, DataControlRowType.DataRow, DataControlRowState.Normal);
                            gvr.ClientIDMode = ClientIDMode.Static;
                            gvr.ID = groupId;
                            gvr.Attributes["data-exgrouphead"] = "true";
                            if (groupCnt > 1 && HideGroupedColumnDef)
                            {
                                gvr.Style["display"] = "none";
                                gvr.Attributes["data-exgroup"] = gridEv.Rows[prevMin].Attributes["data-exgroup"];
                            }
                            for (int colindex = 0; colindex < i; colindex++)
                            {
                                var cc = new TableCell();
                                if (lastgroup != null && lastgroup.Cells.Count > colindex)
                                {
                                    cc.BackColor = lastgroup.Cells[colindex].BackColor;
                                    cc.MergeStyle(GroupedCellStyle);
                                }
                                gvr.Cells.Add(cc);
                            }

                            var groupedCell = new TableHeaderCell() { ColumnSpan = gridEv.HeaderRow.Cells.Cast<TableCell>().Where(c => c.Visible).Count() - i, BackColor = groupColor };
                            groupedCell.MergeStyle(GroupHeaderCellStyle);
                            groupedCell.Attributes["onclick"] = "ExtendedGridView.toggleGroupedRows('" + groupId + "');";
                            gvr.Cells.Add(groupedCell);

                            var divgroup = (HtmlContainerControl)new HtmlGenericControl("div");
                            groupedCell.Controls.Add(divgroup);

                            var grouptitle = new HtmlGenericControl("span") { InnerHtml = ((DataControlFieldCell)gridEv.HeaderRow.Cells[i]).ContainingField.HeaderText + ": " };
                            grouptitle.Attributes["class"] = "ExtendedGridViewGroupTitle";
                            divgroup.Controls.Add(grouptitle);

                            var groupvalue = new HtmlGenericControl("span") { InnerHtml = cell.Text };
                            groupvalue.Attributes["class"] = "ExtendedGridViewGroupValue";
                            divgroup.Controls.Add(groupvalue);

                            gridEv.Rows[prevMin].Parent.Controls.AddAt(prevMin + (HeaderColumnsGroups != null && HeaderColumnsGroups.Any() ? 1 : 0) + boundaries.Count(b => b <= prevMin), gvr);//prevMin + 1 + offset, gvr);

                            lastgroup = gvr;
                        }

                        if (HideCellWhenGroup)
                        {
                            cell.Text = String.Empty;// "g" + boundaries.Count;
                        }

                        if (HideGroupedColumnDef)
                        {
                            r.Style["display"] = "none";
                            r.Attributes["data-exgroup"] = groupId;
                        }

                        cell.BackColor = groupColor;
                        cell.MergeStyle(GroupedCellStyle);
                    }
                }
            }
        }*/

        #endregion Legacy group rows
        
    }
}