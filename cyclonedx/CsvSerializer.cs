using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CycloneDX.Models.v1_2;

namespace CycloneDX.CLI
{
    public static class CsvSerializer
    {
        public static string Serialize(CycloneDX.Models.v1_2.Bom bom)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteField("Type");
                    csv.WriteField("MimeType");
                    csv.WriteField("Supplier");
                    csv.WriteField("Author");
                    csv.WriteField("Publisher");
                    csv.WriteField("Group");
                    csv.WriteField("Name");
                    csv.WriteField("Version");
                    csv.WriteField("Scope");
                    csv.WriteField("LicenseExpressions");
                    csv.WriteField("LicenseNames");
                    csv.WriteField("Copyright");
                    csv.WriteField("Cpe");
                    csv.WriteField("Purl");
                    csv.WriteField("Modified");
                    csv.WriteField("SwidTagId");
                    csv.WriteField("SwidName");
                    csv.WriteField("SwidVersion");
                    csv.WriteField("SwidTagVersion");
                    csv.WriteField("SwidPatch");
                    csv.WriteField("SwidTextContentType");
                    csv.WriteField("SwidTextEncoding");
                    csv.WriteField("SwidTextContent");
                    csv.WriteField("SwidUrl");
                    var hashAlgorithms = Enum.GetValues(typeof(Hash.HashAlgorithm)).Cast<Hash.HashAlgorithm>();
                    foreach (var hashAlgorithm in hashAlgorithms)
                    {
                        csv.WriteField(hashAlgorithm.ToString().Replace('_', '-'));
                    }
                    csv.WriteField("Description");

                    csv.NextRecord();

                    foreach (var c in bom.Components)
                    {
                        csv.WriteField(c.Type);
                        csv.WriteField(c.MimeType);
                        csv.WriteField(c.Supplier?.Name);
                        csv.WriteField(c.Author);
                        csv.WriteField(c.Publisher);
                        csv.WriteField(c.Group);
                        csv.WriteField(c.Name);
                        csv.WriteField(c.Version);
                        csv.WriteField(c.Scope);
                        var licenseExpressions = new List<string>();
                        var LicenseNames = new List<string>();
                        if (c.Licenses != null)
                        foreach (var license in c.Licenses)
                        {
                            if (!string.IsNullOrEmpty(license.Expression))
                            {
                                licenseExpressions.Add(license.Expression);
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(license.License?.Id))
                                {
                                    licenseExpressions.Add(license.License?.Id);
                                }
                                else if (!string.IsNullOrEmpty(license.License?.Name))
                                {
                                    LicenseNames.Add(license.License?.Name.Replace(',', '_'));
                                }
                            }
                        }
                        csv.WriteField(string.Join(',', licenseExpressions));
                        csv.WriteField(string.Join(',', LicenseNames));
                        csv.WriteField(c.Copyright);
                        csv.WriteField(c.Cpe);
                        csv.WriteField(c.Purl);
                        csv.WriteField(c.Modified);
                        csv.WriteField(c.Swid?.TagId);
                        csv.WriteField(c.Swid?.Name);
                        csv.WriteField(c.Swid?.Version);
                        csv.WriteField(c.Swid?.TagVersion);
                        csv.WriteField(c.Swid?.Patch);
                        csv.WriteField(c.Swid?.Text?.ContentType);
                        csv.WriteField(c.Swid?.Text?.Encoding);
                        csv.WriteField(c.Swid?.Text?.Content);
                        csv.WriteField(c.Swid?.Url);
                        foreach (var hashAlgorithm in hashAlgorithms)
                        {
                            csv.WriteField(c.Hashes?.Where(h => h.Alg == hashAlgorithm).FirstOrDefault<Hash>()?.Content);
                        }
                        csv.WriteField(c.Description?.Replace("\r", "").Replace("\n", ""));

                        csv.NextRecord();
                    }
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static Bom Deserialize(string csv)
        {
            using (var stream =  new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv)))
            using (var reader = new StreamReader(stream))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csvReader.Configuration.MissingFieldFound = null;
                csvReader.Configuration.PrepareHeaderForMatch =  (string header, int index) => header.ToLower();
        
                var bom = new Bom();
                bom.Components = new List<Component>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var component = new Component
                    {
                        Type = csvReader.GetField<Component.ComponentType?>("Type") ?? Component.ComponentType.Library,
                        MimeType = csvReader.GetField("MimeType").NullIfWhiteSpace(),
                        // BomRef not supported
                        Supplier = new OrganizationalEntity
                        {
                            // additional supplier information not supported
                            Name = csvReader.GetField("Supplier").NullIfWhiteSpace()
                        },
                        Author = csvReader.GetField("Author").NullIfWhiteSpace(),
                        Publisher = csvReader.GetField("Publisher").NullIfWhiteSpace(),
                        Group = csvReader.GetField("Group").NullIfWhiteSpace(),
                        Name = csvReader.GetField("Name").NullIfWhiteSpace(),
                        Version = csvReader.GetField("Version").NullIfWhiteSpace(),
                        Description = csvReader.GetField("Description").NullIfWhiteSpace(),
                        Scope = csvReader.GetField("Scope").NullIfWhiteSpace(),
                        Copyright = csvReader.GetField("Copyright").NullIfWhiteSpace(),
                        Cpe = csvReader.GetField("Cpe").NullIfWhiteSpace(),
                        Purl = csvReader.GetField("Purl").NullIfWhiteSpace(),
                        Swid = new Swid
                        {
                            TagId = csvReader.GetField("SwidTagId").NullIfWhiteSpace(),
                            Name = csvReader.GetField("SwidName").NullIfWhiteSpace(),
                            Version = csvReader.GetField("SwidVersion").NullIfWhiteSpace(),
                            TagVersion = csvReader.GetField<int?>("SwidTagVersion"),
                            Patch = csvReader.GetField<bool?>("SwidPatch"),
                            Text = new AttachedText
                            {
                                ContentType = csvReader.GetField("SwidTextContentType").NullIfWhiteSpace(),
                                Encoding = csvReader.GetField("SwidTextEncoding").NullIfWhiteSpace(),
                                Content = csvReader.GetField("SwidTextContent".NullIfWhiteSpace())
                            },
                            Url = csvReader.GetField("SwidUrl").NullIfWhiteSpace()
                        },
                        Modified= csvReader.GetField<bool?>("Modified"),
                        // pedigree not supported
                        // external references not supported
                        // sub-components not supported
                    };
                    if (component.Supplier.Name == null) component.Supplier = null;
                    if (component.Swid.Text.Content == null) component.Swid.Text = null;
                    if (component.Swid.TagId == null) component.Swid = null;

                    var hashAlgorithms = Enum.GetValues(typeof(Hash.HashAlgorithm)).Cast<Hash.HashAlgorithm>();
                    var hashes = new List<Hash>();
                    foreach (var hashAlgorithm in hashAlgorithms)
                    {
                        var hash = new Hash();
                        hash.Alg = hashAlgorithm;
                        hash.Content = csvReader.GetField(hashAlgorithm.ToString().Replace('_', '-'));
                        if (!string.IsNullOrEmpty(hash.Content)) hashes.Add(hash);
                    }
                    if (hashes.Count > 0) component.Hashes = hashes;

                    var componentLicenses = new List<ComponentLicense>();
                    var licenseExpressions = csvReader.GetField("LicenseExpressions")?.Split(',');
                    if (licenseExpressions != null)
                    foreach (var licenseExpressionString in licenseExpressions)
                    {
                        if (licenseExpressionString.Contains(" ")) // license expression
                        {
                            var componentLicense = new ComponentLicense
                            {
                                Expression = licenseExpressionString
                            };
                            componentLicenses.Add(componentLicense);
                        }
                        else if (!string.IsNullOrEmpty(licenseExpressionString)) // license ID
                        {
                            var componentLicense = new ComponentLicense
                            {
                                License = new License
                                {
                                    Id = licenseExpressionString
                                }
                            };
                            componentLicenses.Add(componentLicense);
                        }
                    }
                    var licenseNames = csvReader.GetField("LicenseNames")?.Split(',');
                    if (licenseNames != null)
                    foreach (var licenseName in licenseNames)
                    {
                        if (!string.IsNullOrEmpty(licenseName))
                        componentLicenses.Add(new ComponentLicense
                        {
                            License = new License
                            {
                                Name = licenseName
                            }
                        });
                    }
                    if (componentLicenses.Count > 0) component.Licenses = componentLicenses;

                    bom.Components.Add(component);
                }
                return bom;
            }
        }
    }
}