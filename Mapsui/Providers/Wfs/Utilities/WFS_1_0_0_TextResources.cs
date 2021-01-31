// WFS provider by Peter Robineau (peter.robineau@gmx.at)
// This file can be redistributed and/or modified under the terms of the GNU Lesser General Public License.

using System.Collections.Generic;
using Mapsui.Geometries;
using System.IO;
using System.Text;
using System.Xml;
// ReSharper disable InconsistentNaming

namespace Mapsui.Providers.Wfs.Utilities
{
    public class WFS_1_0_0_TextResources : WFS_1_0_0_XPathTextResources, IWFS_TextResources
    {
        
        ////////////////////////////////////////////////////////////////////////
        // HTTP Configuration                                                 //                      
        // POST & GET                                                         //
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the query string for 'GetCapabilities'.
        /// </summary>
        public string GetCapabilitiesRequest()
        {
            return "?SERVICE=WFS&Version=1.0.0&REQUEST=GetCapabilities";
        }

        /// <summary>
        /// This method returns the query string for 'DescribeFeatureType'.
        /// </summary>
        /// <param name="featureTypeName">The name of the featuretype to query</param>
        public string DescribeFeatureTypeRequest(string featureTypeName)
        {
            return "?SERVICE=WFS&Version=1.0.0&REQUEST=DescribeFeatureType&TYPENAME=" + featureTypeName;
        }

        
        
        /// <summary>
        /// This method returns the query string for 'GetFeature'.
        /// </summary>
        /// <param name="featureTypeInfo">A <see cref="WfsFeatureTypeInfo"/> instance providing metadata of the featuretype to query</param>
        /// <param name="labelProperties">A list of properties necessary for label rendering</param>
        /// <param name="boundingBox">The bounding box of the query</param>
        /// <param name="filter">An instance implementing <see cref="IFilter"/></param>
        public string GetFeatureGETRequest(WfsFeatureTypeInfo featureTypeInfo, List<string> labelProperties, BoundingBox boundingBox, IFilter filter)
        {
            string qualification = string.IsNullOrEmpty(featureTypeInfo.Prefix)
                                       ? string.Empty
                                       : featureTypeInfo.Prefix + ":";
            string filterString = string.Empty;

            if (filter != null)
            {
                filterString = filter.Encode();
                filterString = filterString.Replace("<", "%3C");
                filterString = filterString.Replace(">", "%3E");
                filterString = filterString.Replace(" ", "");
                filterString = filterString.Replace("*", "%2a");
                filterString = filterString.Replace("#", "%23");
                filterString = filterString.Replace("!", "%21");
            }

            var filterBuilder = new StringBuilder();
            filterBuilder.Append("&filter=%3CFilter%20xmlns=%22" + NSOGC + "%22%20xmlns:gml=%22" + NSGML + "%22");
            if (!string.IsNullOrEmpty(featureTypeInfo.Prefix))
            {
                filterBuilder.Append("%20xmlns:" + featureTypeInfo.Prefix + "=%22" +
                                     featureTypeInfo.FeatureTypeNamespace + "%22");
                //added by PDD to get it to work for deegree default sample
            }
            filterBuilder.Append("%3E");
            if (boundingBox != null)
            {
                filterBuilder.Append("%3CBBOX%3E%3CPropertyName%3E");
                filterBuilder.Append(qualification).Append(featureTypeInfo.Geometry.GeometryName);
                filterBuilder.Append("%3C/PropertyName%3E");
                filterBuilder.Append("%3Cgml:Box%20srsName=%22" + featureTypeInfo.SRID + "%22%3E");
                filterBuilder.Append("%3Cgml:coordinates%3E");
                filterBuilder.Append(XmlConvert.ToString(boundingBox.Left) + ",");
                filterBuilder.Append(XmlConvert.ToString(boundingBox.Bottom) + "%20");
                filterBuilder.Append(XmlConvert.ToString(boundingBox.Right) + ",");
                filterBuilder.Append(XmlConvert.ToString(boundingBox.Top));
                filterBuilder.Append("%3C/gml:coordinates%3E%3C/gml:Box%3E%3C/BBOX%3E");
            }
            filterBuilder.Append(filterString);
            filterBuilder.Append("%3C/Filter%3E");

            return "?SERVICE=WFS&Version=1.0.0&REQUEST=GetFeature&TYPENAME=" + qualification + featureTypeInfo.Name +
                   "&SRS =" + featureTypeInfo.SRID + filterBuilder;
        }

        /// <summary>
        /// This method returns the POST request for 'GetFeature'.
        /// </summary>
        /// <param name="featureTypeInfo">A <see cref="WfsFeatureTypeInfo"/> instance providing metadata of the featuretype to query</param>
        /// <param name="labelProperties">A list of properties necessary for label rendering</param>
        /// <param name="boundingBox">The bounding box of the query</param>
        /// <param name="filter">An instance implementing <see cref="IFilter"/></param>
        public byte[] GetFeaturePOSTRequest(WfsFeatureTypeInfo featureTypeInfo, List<string> labelProperties,
                                            BoundingBox boundingBox, IFilter filter)
        {
            string qualification = string.IsNullOrEmpty(featureTypeInfo.Prefix)
                                       ? string.Empty
                                       : featureTypeInfo.Prefix + ":";

            using (var sWriter = new StringWriter())
            {
                using (var xWriter = new XmlTextWriter(sWriter))
                {
                    xWriter.Namespaces = true;
                    xWriter.WriteStartElement("GetFeature", NSWFS);
                    xWriter.WriteAttributeString("service", "WFS");
                    xWriter.WriteAttributeString("version", "1.0.0");
                    xWriter.WriteStartElement("Query", NSWFS);
                    xWriter.WriteAttributeString("typeName", qualification + featureTypeInfo.Name);
                    xWriter.WriteElementString("PropertyName", qualification + featureTypeInfo.Geometry.GeometryName);
                    foreach (var labelProperty in labelProperties)
                    {
                        if (!string.IsNullOrEmpty(labelProperty))
                            xWriter.WriteElementString("PropertyName", qualification + labelProperty);
                    }
                    xWriter.WriteStartElement("Filter", NSOGC);
                    if (filter != null && boundingBox != null) xWriter.WriteStartElement("And");
                    if (boundingBox != null)
                    {
                        xWriter.WriteStartElement("BBOX");
                        if (!string.IsNullOrEmpty(featureTypeInfo.Prefix) &&
                            !string.IsNullOrEmpty(featureTypeInfo.FeatureTypeNamespace))
                            xWriter.WriteElementString("PropertyName",
                                qualification + featureTypeInfo.Geometry.GeometryName);
                        //added qualification to get it to work for deegree default sample
                        else
                            xWriter.WriteElementString("PropertyName", featureTypeInfo.Geometry.GeometryName);
                        xWriter.WriteStartElement("gml", "Box", NSGML);
                        xWriter.WriteAttributeString("srsName",
                            "http://www.opengis.net/gml/srs/epsg.xml#" + featureTypeInfo.SRID);
                        xWriter.WriteElementString("coordinates", NSGML,
                            XmlConvert.ToString(boundingBox.Left) + "," +
                            XmlConvert.ToString(boundingBox.Bottom) + " " +
                            XmlConvert.ToString(boundingBox.Right) + "," +
                            XmlConvert.ToString(boundingBox.Top));
                        xWriter.WriteEndElement();
                        xWriter.WriteEndElement();
                    }
                    if (filter != null) xWriter.WriteRaw(filter.Encode());
                    if (filter != null && boundingBox != null) xWriter.WriteEndElement();
                    xWriter.WriteEndElement();
                    xWriter.WriteEndElement();
                    xWriter.WriteEndElement();
                    xWriter.Flush();
                    return Encoding.UTF8.GetBytes(sWriter.ToString());
                }
            }
        }

    }
}