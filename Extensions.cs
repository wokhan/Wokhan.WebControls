using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections;
using System.Data;

namespace Wokhan.WebControls
{
    internal static class Extensions
    {
        public static int IndexOf<T>(this IEnumerable<T> src, T obj)
        {
            return src.Select((o, i) => new { o, i })
                      .Where(oi => oi.o.Equals(obj))
                      .Select(oi => oi.i)
                      .DefaultIfEmpty(-1)
                      .FirstOrDefault();
        }

        public static bool IsArrayEnumerable(this IEnumerable src)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            Type t = src.GetType();

            return (t.HasElementType && t.GetElementType().IsArray) || (t.GetGenericArguments().Length > 0 && t.GetGenericArguments()[0].IsArray);
        }

        public static Type GetEnumerationInnerType(this object src)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            Type srcType = src.GetType();
            if (src is IEnumerable)
            {
                if (srcType.HasElementType)
                {
                    return srcType.GetElementType();
                }
                else if (srcType.GetGenericArguments().Length > 0)
                {
                    return srcType.GetGenericArguments()[0].GetElementType();
                }
            }

            return null;
        }

        public static Type GetInnerType<T>(this IEnumerable<T> src)
        {
            return typeof(T);
        }

        public static void RegisterCSS(this WebControl wc, string resourceName)
        {
            if (!wc.Page.Items.Contains(resourceName))
            {
                wc.Page.Items.Add(resourceName, true);

                string csslnk = String.Format("<link href='{0}' rel='stylesheet' type='text/css' />", wc.Page.ClientScript.GetWebResourceUrl(wc.GetType(), resourceName));
                LiteralControl lcLiteralControl = new LiteralControl(csslnk);
                wc.Page.Header.Controls.AddAt(0, lcLiteralControl);
            }
        }

        public static object ApplyFilter(this object localSource, Dictionary<string, string> SearchTerms)
        {
            if (localSource is DataView)
            {
                ((DataView)localSource).ApplyFilter(SearchTerms);
            }
            else if (localSource is IEnumerable)
            {
                if (((IEnumerable)localSource).IsArrayEnumerable())
                {
                    localSource = ((IEnumerable)localSource).Cast<object[]>().ApplyFilter(SearchTerms);
                }
                else
                {
                    localSource = ((IEnumerable)localSource).Cast<object>().ApplyFilter(SearchTerms);
                }
            }
            else if (localSource is DataTable)
            {
                ((DataTable)localSource).ApplyFilter(SearchTerms);
            }

            return localSource;
        }

        public static void ApplyFilter(this DataTable src, Dictionary<string, string> searchTerms)
        {
            src.DefaultView.ApplyFilter(searchTerms);
        }

        public static void ApplyFilter(this DataView src, Dictionary<string, string> searchTerms)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            if (searchTerms == null || searchTerms.Count == 0)
            {
                src.RowFilter = String.Empty;
                return;
            }

            // Sanitizing the values (escaping special chars)
            searchTerms = searchTerms.ToDictionary(a => a.Key, a => a.Value.Replace("'", "''").Replace("[", "[[]").Replace("]", "[]]").Replace("%", "[%]").Replace("*", "[*]"));

            if ((searchTerms.Count == 1 && String.IsNullOrEmpty(searchTerms.First().Key)))
            {
                src.RowFilter = src.Table.Columns.Cast<DataColumn>()
                                                 .Select(c => (c.DataType == typeof(string) ? "[" + c.ColumnName + "]" : "Convert(" + c.ColumnName + ", 'System.String')") + " LIKE '*" + searchTerms.First().Value + "*'")
                                                 .Aggregate((a, b) => a + " OR " + b);

            }
            else
            {
                src.RowFilter = searchTerms.Select(f => (src.Table.Columns[f.Key].DataType == typeof(string) ? "[" + f.Key + "]" : "Convert(" + f + ", 'System.String')") + " LIKE '*" + f.Value + "*'")
                                           .Aggregate((a, b) => a + " AND " + b);

            }
        }



        public static IEnumerable<T> ApplyFilter<T>(this IEnumerable<T> src, Dictionary<string, string> searchTerms)
        {
            IEnumerable<T> ret;

            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            if (searchTerms == null || searchTerms.Count == 0)
            {
                return src;
            }

            Func<object, string, object> getVal;
            Func<object, IEnumerable<object>> getAllVal;

            if (src is EnumerableRowCollection)
            {
                getVal = (o, i) => ((DataRow)o)[i];
                getAllVal = (o) => ((DataRow)o).ItemArray;
            }
            else if (((IEnumerable)src).IsArrayEnumerable())
            {
                getVal = (o, i) => ((object[])o)[int.Parse(i)];
                getAllVal = (o) => ((object[])o);
            }
            else
            {
                getVal = (o, p) => o.GetType().GetProperty(p).GetValue(o, null);
                getAllVal = (o) => o.GetType().GetProperties().Select(f => f.GetValue(o, null));
            }

            if ((searchTerms.Count == 1 && String.IsNullOrEmpty(searchTerms.First().Key)))
            {
                ret = src.Where(o => getAllVal(o).Any(v => v != null && v.ToString().IndexOf(searchTerms.First().Value, StringComparison.InvariantCultureIgnoreCase) != -1));
            }
            else
            {
                ret = src.Where(o => searchTerms.Any(f => getVal(o, f.Key) != null && getVal(o, f.Key).ToString().IndexOf(f.Value, StringComparison.InvariantCultureIgnoreCase) != -1));
            }

            return ret;
        }

        public static object ApplySort(this object localSource, Dictionary<string, SortDirection> SortColumns) 
        {
            if (localSource is DataView)
            {
                ((DataView)localSource).ApplySort(SortColumns);
            }
            else if (localSource is IEnumerable)
            {
                if (((IEnumerable)localSource).IsArrayEnumerable())
                {
                    localSource = ((IEnumerable)localSource).Cast<object[]>().ApplySort(SortColumns);
                }
                else
                {
                    localSource = ((IEnumerable)localSource).Cast<object>().ApplySort(SortColumns);
                }

            }
            else if (localSource is DataTable)
            {
                ((DataTable)localSource).ApplySort(SortColumns);
            }

            return localSource;
        }

        public static void ApplySort(this DataTable src, Dictionary<string, SortDirection> SortColumns)
        {
            src.DefaultView.ApplySort(SortColumns);
        }

        public static void ApplySort(this DataView src, Dictionary<string, SortDirection> SortColumns)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            src.Sort = SortColumns.Select(kv => kv.Key + " " + (kv.Value == SortDirection.Ascending ? "ASC" : "DESC"))
                                  .Aggregate((a, b) => a + ", " + b);
        }

        public static IOrderedEnumerable<T> ApplySort<T>(this IEnumerable<T> src, Dictionary<string, SortDirection> SortColumns)
        {
            IOrderedEnumerable<T> ret = null;

            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            if (SortColumns == null || SortColumns.Count == 0)
            {
                throw new ArgumentException("Collection can not be null nor empty.", "SortColumns");
            }

            Func<object, string, object> getVal;
            Func<object, IEnumerable<object>> getAllVal;

            if (src is EnumerableRowCollection)
            {
                getVal = (o, i) => ((DataRow)o)[i];
                getAllVal = (o) => ((DataRow)o).ItemArray;
            }
            else if (((IEnumerable)src).IsArrayEnumerable())
            {
                getVal = (o, i) => ((object[])o)[int.Parse(i)];
                getAllVal = (o) => ((object[])o);
            }
            else
            {
                getVal = (o, p) => o.GetType().GetProperty(p).GetValue(o, null);
                getAllVal = (o) => o.GetType().GetProperties().Select(f => f.GetValue(o, null));
            }

            foreach (KeyValuePair<string, SortDirection> colSort in SortColumns)
            {
                ret = src.ContextualThenBy((o) => getVal(o, colSort.Key), colSort.Value == SortDirection.Ascending);
            }

            return ret;

        }

        public static IOrderedEnumerable<T> ContextualThenBy<T>(this IEnumerable<T> src, Func<T, object> sorter, bool ascending)
        {
            if (src is IOrderedEnumerable<T>)
            {
                return ascending ? ((IOrderedEnumerable<T>)src).ThenBy(sorter) : ((IOrderedEnumerable<T>)src).ThenByDescending(sorter);
            }
            else
            {
                return ascending ? src.OrderBy(sorter) : src.OrderByDescending(sorter);
            }
        }

        public static string GetUnderlyingField(this DataControlFieldCell src)
        {
            if (src.ContainingField is BoundField)
            {
                var r = ((BoundField)src.ContainingField).DataField;
                if (r != "!")
                {
                    return r;
                }
            }

            return src.ContainingField.ToString();
        }
    }
}
