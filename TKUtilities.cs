using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TeamkistPlugin
{
    public static class TKUtilities
    {
        public static List<float> PropertyStringToList(string properties)
        {
            return properties.Split('|').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToList();
        }

        public static string PropertyListToString(List<float> properties)
        {
            return string.Join("|", properties.Select(p => p.ToString(CultureInfo.InvariantCulture)));
        }

        public static void AssignPropertyListToBlockPropertyJSON(string properties, BlockPropertyJSON blockPropertyJSON)
        {
            List<float> propertyList = PropertyStringToList(properties);

            blockPropertyJSON.position.x = propertyList[0];
            blockPropertyJSON.position.y = propertyList[1];
            blockPropertyJSON.position.z = propertyList[2];
            blockPropertyJSON.eulerAngles.x = propertyList[3];
            blockPropertyJSON.eulerAngles.y = propertyList[4];
            blockPropertyJSON.eulerAngles.z = propertyList[5];
            blockPropertyJSON.localScale.x = propertyList[6];
            blockPropertyJSON.localScale.y = propertyList[7];
            blockPropertyJSON.localScale.z = propertyList[8];
            blockPropertyJSON.properties = propertyList;
        }

        public static string FixMissingJSONProperties(string blockJSON)
        {
            BlockPropertyJSON block = LEV_UndoRedo.GetJSONblock(blockJSON);
            block.properties[0] = block.position.x;
            block.properties[1] = block.position.y;
            block.properties[2] = block.position.z;
            block.properties[3] = block.eulerAngles.x;
            block.properties[4] = block.eulerAngles.y;
            block.properties[5] = block.eulerAngles.z;
            block.properties[6] = block.localScale.x;
            block.properties[7] = block.localScale.y;
            block.properties[8] = block.localScale.z;
            return LEV_UndoRedo.GetJSONstring(block);
        }

        public static string FixedPropertyListToString(string blockJSON)
        {
            BlockPropertyJSON block = LEV_UndoRedo.GetJSONblock(blockJSON);
            block.properties[0] = block.position.x;
            block.properties[1] = block.position.y;
            block.properties[2] = block.position.z;
            block.properties[3] = block.eulerAngles.x;
            block.properties[4] = block.eulerAngles.y;
            block.properties[5] = block.eulerAngles.z;
            block.properties[6] = block.localScale.x;
            block.properties[7] = block.localScale.y;
            block.properties[8] = block.localScale.z;
            return PropertyListToString(block.properties);
        }
    }
}
