using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Instant Craft Switchable", "Vlad-0003 / Orange / rostov114 / l3rady", "2.3.0")]
    [Description("Allows players to instantly craft items with features")]
    public class InstantCraftSwitchable : RustPlugin
    {
        #region Vars
        private const string permUse = "InstantCraftSwitchable.use";
        private const string permUseOff = "InstantCraftSwitchable.off";
        #endregion

        #region Oxide Hooks
        private void Init()
        {
            if(_config.switchable)
            {
                AddLocalizedCommand(nameof(SwitchableCommand));
            }

            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permUseOff, this);
        }

        private object OnItemCraft(ItemCraftTask task)
        {
            if (task.cancelled)
            {
                return null;
            }

            if (!permission.UserHasPermission(task.owner.UserIDString, permUse))
            {
                return null;
            }

            // If user has turned off instant craft
            if (permission.UserHasPermission(task.owner.UserIDString, permUseOff))
            {
                return null;
            }

            if (_config.IsBlocked(task))
            {
                CancelTask(task, "Blocked");
                return false;
            }

            List<int> stacks = GetStacks(task.blueprint.targetItem, task.amount * task.blueprint.amountToCreate);
            int slots = FreeSlots(task.owner);
            if (!HasPlace(slots, stacks))
            {
                CancelTask(task, "Slots", stacks.Count, slots);
                return false;
            }

            if (_config.IsNormal(task))
            {
                Message(task.owner, "Normal");
                return null;
            }

            if (!GiveItem(task, stacks))
            {
                return null;
            }

            return true;
        }
        #endregion

        #region Helpers
        private void CancelTask(ItemCraftTask task, string reason, params object[] args)
        {
            task.cancelled = true;
            Message(task.owner, reason, args);
            GiveRefund(task);
            Interface.CallHook("OnItemCraftCancelled", task);
        }

        private void GiveRefund(ItemCraftTask task)
        {
            if (task.takenItems != null && task.takenItems.Count > 0)
            {
                foreach (var item in task.takenItems)
                {
                    task.owner.inventory.GiveItem(item, null);
                }
            }
        }

        private bool GiveItem(ItemCraftTask task, List<int> stacks)
        {
            ulong skin = ItemDefinition.FindSkin(task.blueprint.targetItem.itemid, task.skinID);
            int iteration = 0;

            if (_config.split)
            {
                foreach (var stack in stacks)
                {
                    if (!Give(task, stack, skin) && iteration <= 0)
                    {
                        return false;
                    }

                    iteration++;
                }
            }
            else
            {
                int final = 0;
                foreach (var stack in stacks)
                {
                    final += stack;
                }

                if (!Give(task, final, skin))
                {
                    return false;
                }
            }

            task.cancelled = true;
            return true;
        }

        private bool Give(ItemCraftTask task, int amount, ulong skin)
        {
            Item item = null;
            try
            {
                item = ItemManager.CreateByItemID(task.blueprint.targetItem.itemid, amount, skin);
            }
            catch (Exception e)
            {
                PrintError($"Exception creating item! targetItem: {task.blueprint.targetItem}-{amount}-{skin}; Exception: {e}");
            }

            if (item == null)
            {
                return false;
            }

            if (item.hasCondition && task.conditionScale != 1f)
            {
                item.maxCondition *= task.conditionScale;
                item.condition = item.maxCondition;
            }

            item.OnVirginSpawn();

            if (task.instanceData != null)
            {
                item.instanceData = task.instanceData;
            }

            Interface.CallHook("OnItemCraftFinished", task, item);

            if (task.owner.inventory.GiveItem(item, null))
            {
                task.owner.Command("note.inv", new object[]{item.info.itemid, item.amount});
                return true;
            }

            ItemContainer itemContainer = task.owner.inventory.crafting.containers.First<ItemContainer>();
            task.owner.Command("note.inv", new object[]{item.info.itemid, item.amount});
            task.owner.Command("note.inv", new object[]{item.info.itemid, -item.amount});
            item.Drop(itemContainer.dropPosition, itemContainer.dropVelocity, default(Quaternion));

            return true;
        }

        private int FreeSlots(BasePlayer player)
        {
            var slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
            var taken = player.inventory.containerMain.itemList.Count + player.inventory.containerBelt.itemList.Count;
            return slots - taken;
        }

        private List<int> GetStacks(ItemDefinition item, int amount) 
        {
            var list = new List<int>();
            var maxStack = item.stackable;

            if (maxStack == 0)
            {
                maxStack = 1;
            }

            while (amount > maxStack)
            {
                amount -= maxStack;
                list.Add(maxStack);
            }
            
            list.Add(amount);
            
            return list; 
        }

        private bool HasPlace(int slots, List<int> stacks)
        {
            if (!_config.checkPlace)
            {
                return true;
            }

            if (_config.split && slots - stacks.Count < 0)
            {
                return false;
            }

            return slots > 0;
        }
        #endregion

        #region Localization 2.3.0
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Blocked", "Crafting of that item is blocked!"},
                {"Slots", "You don't have enough place to craft! Need {0}, have {1}!"},
                {"Normal", "Item will be crafted with normal speed."},
                {"SwitchableCommand", "ic"},
                {"NoSwitchablePerms", "You do not have permission to use Instant Craft"},
                {"TurnedOff", "Instant Craft turned off"},
                {"TurnedOn", "Instant Craft turned on"}
            }, this, "en");
        }

        private void Message(BasePlayer player, string messageKey, params object[] args)
        {
            if (player == null)
            {
                return;
            }

            var message = GetMessage(messageKey, player.UserIDString, args);
            player.ChatMessage(message);
        }

        private string GetMessage(string messageKey, string playerID, params object[] args)
        {
            return string.Format(lang.GetMessage(messageKey, this, playerID), args);
        }
        #endregion
        
        #region Configuration 2.3.0
        private Configuration _config;
        private class Configuration
        {
            [JsonProperty(PropertyName = "Check for free place")]
            public bool checkPlace = true;
            
            [JsonProperty(PropertyName = "Split crafted stacks")]
            public bool split = true;

            [JsonProperty(PropertyName = "Allow users to switch instant craft off/on")]
            public bool switchable = true;
            
            [JsonProperty(PropertyName = "Normal Speed")]
            public string[] normal =
            {
                "put item shortname here"
            };

            [JsonProperty(PropertyName = "Blocked items")]
            public string[] blocked =
            {
                "put item shortname here"
            };

            public bool IsNormal(ItemCraftTask task) => normal?.Contains(task.blueprint.targetItem.shortname) ?? false;
            public bool IsBlocked(ItemCraftTask task) => blocked?.Contains(task.blueprint.targetItem.shortname) ?? false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<Configuration>();
                SaveConfig();
            }
            catch
            {
                PrintError("Error reading config, please check!");

                Unsubscribe(nameof(OnItemCraft));
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        private void AddLocalizedCommand(string command)
        {
            foreach (string language in lang.GetLanguages(this))
            {
                Dictionary<string, string> messages = lang.GetMessages(language, this);
                foreach (KeyValuePair<string, string> message in messages)
                {
                    if (!message.Key.Equals(command)) continue;

                    if (string.IsNullOrEmpty(message.Value)) continue;

                    AddCovalenceCommand(message.Value, command);
                }
            }
        }

        private void SwitchableCommand(IPlayer iplayer, string command, string[] args) {
            BasePlayer player = (BasePlayer)iplayer.Object;
            if (player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, permUse))
            {
                Message(player, "NoSwitchablePerms");
                return;
            }

            bool switchOff;

            if(args.Length == 1 && args[0] == "on")
            {
                switchOff = false;
            }
            else if(args.Length == 1 && args[0] == "off")
            {
                switchOff = true;
            }
            else
            {
                if(permission.UserHasPermission(player.UserIDString, permUseOff))
                {
                    switchOff = false;
                }
                else
                {
                    switchOff = true;
                }
            }

            if(switchOff)
            {
                permission.GrantUserPermission(player.UserIDString, permUseOff, this);
                Message(player, "TurnedOff");
            }
            else
            {
                permission.RevokeUserPermission(player.UserIDString, permUseOff);
                Message(player, "TurnedOn");
            }
        }
    }
}