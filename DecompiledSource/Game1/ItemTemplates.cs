namespace Game1;

public class ItemTemplates
{
	public static MyHashTable itemTemplates = new MyHashTable();

	public static void add(ItemTemplate it)
	{
		itemTemplates.put(it.id, it);
	}

	public static ItemTemplate get(short id)
	{
		ItemTemplate itemTemplate = (ItemTemplate)itemTemplates.get(id);
		if (itemTemplate == null)
		{
			itemTemplate = new ItemTemplate(id, 5, 0, "Unknown Item " + id, "No description", 0, 0, 1, -1, isUpToUp: false);
			itemTemplates.put(id, itemTemplate);
		}
		return itemTemplate;
	}
}
