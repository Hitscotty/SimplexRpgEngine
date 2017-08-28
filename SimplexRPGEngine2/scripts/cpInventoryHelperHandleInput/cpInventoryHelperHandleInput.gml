/// @fucntion cpInventoryHelperHandleInput(containerID, index, hover, shiftAction)
/// @desc Handles input of target container
/// @arg {object} containerID ID of container
/// @arg {int} index Current slot
/// @arg {bool} hover If mouse hover over indexed slot
/// @arg {string} shiftAction Action to do when shift + lmb ["equip", "inventoryPick"]

var tmp_id, tmp_i, tmp_hover, tmp_shiftAction;
tmp_id = 0; 
tmp_i = 0;
tmp_hover = false;
tmp_shiftAction = "equip";

if (argument_count > 0) {tmp_id = argument[0];}
if (argument_count > 1) {tmp_i = argument[1];}
if (argument_count > 2) {tmp_hover = argument[2];}
if (argument_count > 3) {tmp_shiftAction = argument[3];}


// We can drag item now
if (tmp_hover)
{
	// Drag item
	if ((mouse_check_button_pressed(mb_left) || mouse_check_button_pressed(mb_middle)) && oHUD.v_mouseFree && !tmp_id.v_slot[tmp_i, e_inventoryAtributes.valBeingUsed] && tmp_id.v_menuItem == -1)
	{
		if (tmp_id.v_itemMouse == -1)
		{
			if (mouse_check_button_pressed(mb_left))
			{
				if (tmp_id.v_slot[tmp_i, e_inventoryAtributes.valID] != e_items.valNONE)
				{
					// Fast equip
					if (keyboard_check_direct(vk_shift))
					{
						if (tmp_shiftAction == "equip")
						{
							for (var i = 0; i < mcEquipmentSlots; i++)
							{
								if (v_equipmentSlots[i, 2] == v_slot[tmp_i, e_inventoryAtributes.valEquipSlot])
								{
									tmp_id.v_slotBeingDragged = tmp_i;
									cpEquipmentEquip(i);
									tmp_id.v_slotBeingDragged = -1;
									break;
								}
							}
						}
						
						if (tmp_shiftAction == "inventoryPick")
						{
							var tmp_freeSlot = cpInventoryHelperFindFreeSlot(oInventory.id);
							cpInventoryHelperSwitch(id, oInventory.id, tmp_i, tmp_freeSlot);									
						}
					}
					else
					{
						tmp_id.v_slotBeingDragged = tmp_i;
						oHUD.v_mouseFree = false;
					}
				}
			}
		}
		else
		{
			if (tmp_id.v_slot[tmp_i, e_inventoryAtributes.valID] == e_items.valNONE)
			{
				for (var j = 0; j <= mcInvenotryAtributes; j++)
				{
					tmp_id.v_slot[tmp_i, j] = tmp_id.v_slot[tmp_id.v_itemMouse, j];
				}
					
				for (var j = 0; j <= mcInventoryProperties; j++)
				{
					tmp_id.v_slotProperty[tmp_i, j] = tmp_id.v_slotProperty[tmp_id.v_itemMouse, j];
					tmp_id.v_slotReq[tmp_i, j] = tmp_id.v_slotReq[tmp_id.v_itemMouse, j];
				}
					
				tmp_id.v_slot[tmp_i, e_inventoryAtributes.valCurrentStackSize] = tmp_id.v_splitRemainning;
				tmp_id.v_itemMouse = -1;
				oHUD.v_mouseFree = true;
			}				
		}
			
	}
		
	// Split item stack
	if (mouse_check_button_pressed(mb_middle) && tmp_id.v_itemMouse == -1 && oHUD.v_mouseFree)
	{
		if (tmp_id.v_slot[tmp_i, e_inventoryAtributes.valCurrentStackSize] >= 2)
		{
			var tmp_number;
				
			tmp_id.v_splitNumber = tmp_id.v_slot[tmp_i, e_inventoryAtributes.valCurrentStackSize] div 2;
			tmp_id.v_splitRemainning = tmp_id.v_slot[tmp_i, e_inventoryAtributes.valCurrentStackSize] - tmp_id.v_splitNumber;
				
			tmp_number = tmp_id.v_splitNumber;
			tmp_id.v_splitNumber = tmp_id.v_splitRemainning;
			tmp_id.v_splitRemainning = tmp_number;
				
			tmp_id.v_itemMouse = tmp_i;	
			tmp_id.v_slot[tmp_i, e_inventoryAtributes.valCurrentStackSize] = tmp_id.v_splitNumber;			
		}
	}				
	else if ((mouse_check_button_pressed(mb_middle) || mouse_check_button_pressed(mb_left)) && tmp_id.v_itemMouse != -1) // Join item stack
	{
		if (tmp_id.v_slot[tmp_i, e_inventoryAtributes.valID] == tmp_id.v_slot[tmp_i, e_inventoryAtributes.valID])
		{
			var tmp_done;
			tmp_done = true;
				
			repeat(tmp_id.v_splitRemainning)
			{
				if (tmp_id.v_slot[tmp_i, e_inventoryAtributes.valCurrentStackSize] < tmp_id.v_slot[tmp_i, e_inventoryAtributes.valMaxStackSize])
				{
					tmp_id.v_slot[tmp_i, e_inventoryAtributes.valCurrentStackSize]++;
					tmp_id.v_splitRemainning--;
				}
				else
				{
					tmp_done = false;
					break;
				}
			}
				
			if (tmp_done)
			{
				tmp_id.v_itemMouse = -1;
				oHUD.v_mouseFree = true;
			}
		}
	}		
}