using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AP2.DataverseAzureAI.Metadata;

public class Publisher
{
    public object address2_line1 { get; set; }
    public object pinpointpublisherdefaultlocale { get; set; }
    public object address1_county { get; set; }
    public object address2_utcoffset { get; set; }
    public object address2_fax { get; set; }
    public DateTime modifiedon { get; set; }
    public object entityimage_url { get; set; }
    public object address1_name { get; set; }
    public object address1_line1 { get; set; }
    public string uniquename { get; set; }
    public object address1_postalcode { get; set; }
    public object address2_line3 { get; set; }
    public string address1_addressid { get; set; }
    public string publisherid { get; set; }
    public object address1_line3 { get; set; }
    public object address2_name { get; set; }
    public object address2_city { get; set; }
    public object address1_utcoffset { get; set; }
    public object pinpointpublisherid { get; set; }
    public object address2_county { get; set; }
    public object emailaddress { get; set; }
    public object address2_postofficebox { get; set; }
    public object address1_stateorprovince { get; set; }
    public object address2_telephone3 { get; set; }
    public object address2_addresstypecode { get; set; }
    public object address2_telephone2 { get; set; }
    public object address2_telephone1 { get; set; }
    public object address2_shippingmethodcode { get; set; }
    public string _modifiedonbehalfby_value { get; set; }
    public bool isreadonly { get; set; }
    public object address2_stateorprovince { get; set; }
    public object entityimage_timestamp { get; set; }
    public object address1_latitude { get; set; }
    public int customizationoptionvalueprefix { get; set; }
    public object address2_latitude { get; set; }
    public object address1_longitude { get; set; }
    public object address1_line2 { get; set; }
    public string FriendlyName { get; set; }
    public string supportingwebsiteurl { get; set; }
    public object address2_line2 { get; set; }
    public object address2_postalcode { get; set; }
    public string _organizationid_value { get; set; }
    public long versionnumber { get; set; }
    public object address2_upszone { get; set; }
    public object address2_longitude { get; set; }
    public object address1_fax { get; set; }
    public string customizationprefix { get; set; }
    public object _createdonbehalfby_value { get; set; }
    public string _modifiedby_value { get; set; }
    public DateTime createdon { get; set; }
    public object address2_country { get; set; }
    public string description { get; set; }
    public string address2_addressid { get; set; }
    public object address1_shippingmethodcode { get; set; }
    public object address1_postofficebox { get; set; }
    public object address1_upszone { get; set; }
    public object address1_addresstypecode { get; set; }
    public object address1_country { get; set; }
    public object entityimageid { get; set; }
    public object entityimage { get; set; }
    public string _createdby_value { get; set; }
    public object address1_telephone3 { get; set; }
    public object address1_city { get; set; }
    public object address1_telephone2 { get; set; }
    public object address1_telephone1 { get; set; }

    public override string ToString()
    {
        return FriendlyName;
    }
}
