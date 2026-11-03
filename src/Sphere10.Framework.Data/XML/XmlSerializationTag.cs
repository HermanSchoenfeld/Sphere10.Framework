// -----------------------------------------------------------------------------------
// Use it as you please, but keep this header.
// Author : Marcus Deecke, 2006
// Web    : www.yaowi.com
// Email  : code@yaowi.com
// http://www.codeproject.com/KB/XML/deepserializer.aspx
// -----------------------------------------------------------------------------------

namespace Sphere10.Framework.Data;

/// <summary>
/// This class supports the Yaowi Framework infrastructure and is not intended to be used directly from your code. 
/// <P>These constants are used to parse the XmlNodes.
/// </summary>
public class XmlSerializationTag : IXmlSerializationTag {
	private string OBJECT = "object";
	private string NAME = "name";
	private string TYPE = "type";
	private string ASSEMBLY = "assembly";
	private string PROPERTIES = "properties";
	private string PROPERTY = "property";
	private string ITEMS = "items";
	private string ITEM = "item";
	private string INDEX = "index";
	private string NAME_ATT_KEY = "Key";
	private string NAME_ATT_VALUE = "Value";
	private string TYPE_DICTIONARY = "typedictionary";
	private string GENERIC_TYPE_ARGUMENTS = "generictypearguments";

	public virtual string GENERIC_TYPE_ARGUMENTS_TAG => GENERIC_TYPE_ARGUMENTS;

	public virtual string OBJECT_TAG => OBJECT;

	public virtual string NAME_TAG => NAME;

	public virtual string TYPE_TAG => TYPE;

	public virtual string ASSEMBLY_TAG => ASSEMBLY;

	public virtual string PROPERTIES_TAG => PROPERTIES;

	public virtual string PROPERTY_TAG => PROPERTY;

	public virtual string ITEMS_TAG => ITEMS;

	public virtual string ITEM_TAG => ITEM;

	public virtual string INDEX_TAG => INDEX;

	public virtual string NAME_ATT_KEY_TAG => NAME_ATT_KEY;

	public virtual string NAME_ATT_VALUE_TAG => NAME_ATT_VALUE;

	public virtual string TYPE_DICTIONARY_TAG => TYPE_DICTIONARY;

	public string CONSTRUCTOR_TAG { get; } = "constructor";

	public string BINARY_DATA_TAG { get; } = "binarydata";

}

