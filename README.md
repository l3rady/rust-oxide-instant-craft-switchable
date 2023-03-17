This plugin allows players to craft instantly (with permission). It makes the craft time of every item in-game instant, no matter of how many items players are crafting at the same time.

# Features

* block configurable items from crafting
* configurable items to be crafted normally
* check for space in inventory for craft
* split crafted items
* works with custom inventory limits
* localizable command to allow player to turn off/on instant craft

# Permissions

* To use instant craft - InstantCraftSwitchable.use
* To disable player instant craft (internal use only, do not manually assign to users/groups) - InstantCraftSwitchable.off

# Configuration

The settings and options can be configured in the InstantCraftSwitchable file under the config directory. The use of an editor and validator is recommended to avoid formatting issues and syntax errors.

```json
{
    "Check for free place": false,
    "Split crafted stacks": true,
    "Allow users to switch instant craft off/on": true,
    "Normal Speed": [
        "put item shortname here",
        "put another item shortname here",
        "etc"
    ],
    "Blocked items": [
        "put item shortname here",
        "put another item shortname here",
        "etc"
    ]
}
```

# Commands

Commands only work if user has the InstantCraftSwitchable.use permission. The `/ic` command can be changed and translated via editing the language files.

* `/ic` - Toggles instant craft on/off
* `/ic on` - Turns on instant craft
* `/ic off` - Turns off instant craft

# Credits
* Vlad-0003, the original developer of the plugin
* Orange & rostov114 for previous development