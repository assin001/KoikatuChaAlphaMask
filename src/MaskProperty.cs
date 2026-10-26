using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Sideloader;
using Sideloader.AutoResolver;

namespace KK_ChaAlphaMask;

[Serializable]
[XmlRoot("mask")]
public class MaskProperty
{
	[XmlElement("guid")]
	public string Guid { get; set; }

	[XmlElement("category")]
	public int CategoryNo { get; set; }

	[XmlElement("botMask")]
	public bool BotMask { get; set; }

	[XmlElement("id")]
	public int ID { get; set; }

	[XmlElement("objOpt01", IsNullable = true)]
	public bool? ObjOpt01 { get; set; }

	[XmlElement("objOpt02", IsNullable = true)]
	public bool? ObjOpt02 { get; set; }

	[XmlElement("pngBodyMaskPath")]
	public string PngBodyMaskPath { get; set; }

	[XmlElement("abPath")]
	public string ABPath { get; set; }

	[XmlElement("abBodyMaskName")]
	public string ABBodyMask { get; set; }

	[XmlIgnore]
	public int LocalSlot { get; set; }

	[XmlIgnore]
	public static List<MaskProperty> DataSet { get; private set; } = new List<MaskProperty>();

	internal static void Init(IEnumerable<XDocument> manifests)
	{
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		IEnumerable<XElement> enumerable = from x in manifests
			where x.Root.Element("ChaAlphaMask") != null
			select x.Root.Element("ChaAlphaMask");
		if (enumerable == null)
		{
			return;
		}
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(MaskProperty));
		MaskProperty maskProperty = new MaskProperty();
		foreach (IEnumerable<XElement> item in enumerable.Select((XElement x) => x.Elements("mask")))
		{
			foreach (XElement item2 in item)
			{
				try
				{
					XmlReader xmlReader = item2.CreateReader();
					maskProperty = (MaskProperty)xmlSerializer.Deserialize(xmlReader);
					if (string.IsNullOrEmpty(maskProperty.ABBodyMask))
					{
						if (global::Sideloader.Sideloader.IsPng("abdata/" + maskProperty.PngBodyMaskPath))
						{
							maskProperty.PngBodyMaskPath = "abdata/" + maskProperty.PngBodyMaskPath;
							goto IL_01d8;
						}
						ChaAlphaMask.Logger.LogInfo((object)$"Mask Texture missing. guid:{maskProperty.Guid} category:{maskProperty.CategoryNo} id:{maskProperty.ID}");
						xmlReader.Close();
					}
					else if (string.IsNullOrEmpty(maskProperty.ABPath))
					{
						ChaAlphaMask.Logger.LogInfo((object)$"bundleName Empty. guid:{maskProperty.Guid} category:{maskProperty.CategoryNo} id:{maskProperty.ID}");
						xmlReader.Close();
					}
					else
					{
						if (AssetBundleCheck.IsFile(maskProperty.ABPath, ""))
						{
							goto IL_01d8;
						}
						ChaAlphaMask.Logger.LogInfo((object)$"AssetBundle missing. guid:{maskProperty.Guid} category:{maskProperty.CategoryNo} id:{maskProperty.ID}");
						xmlReader.Close();
					}
					goto end_IL_00af;
					IL_01d8:
					maskProperty.Guid = (string.IsNullOrEmpty(maskProperty.Guid) ? item2.Parent.Parent.Element("guid").Value : maskProperty.Guid);
					MaskProperty maskProperty2 = maskProperty;
					ResolveInfo val = UniversalAutoResolver.TryGetResolutionInfo(maskProperty.ID, (ChaListDefine.CategoryNo)Enum.ToObject(typeof(ChaListDefine.CategoryNo), maskProperty.CategoryNo), maskProperty.Guid);
					maskProperty2.LocalSlot = ((val != null) ? val.LocalSlot : maskProperty.ID);
					xmlReader.Close();
					DataSet.Add(maskProperty);
					end_IL_00af:;
				}
				catch (Exception ex)
				{
					ChaAlphaMask.Logger.LogError((object)ex);
				}
			}
		}
	}
}
