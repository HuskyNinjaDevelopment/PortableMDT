using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableMDT
{
    internal class PortableMDT : Plugin
    {
        //Prop Data
        private string _propName = "prop_cs_tablet";
        private int _prop = 0;

        //Anim Data
        private string _animDictionary = "amb@world_human_seat_wall_tablet@female@idle_a";
        private string _animName = "idle_c";
        private int _animFlag = 31;

        internal PortableMDT()
        {
            Init();

            BuildCommand();
            AddEventHandlers();
        }

        private async void Init()
        {
            //Load the Model for the Tablet
            API.RequestModel((uint)API.GetHashKey(_propName));
            while(!API.HasModelLoaded((uint)API.GetHashKey(_propName))) { await Delay(5); }

            //Load the Animation for using the tablet
            API.RequestAnimDict(_animDictionary);
            while(!API.HasAnimDictLoaded(_animDictionary)) { await Delay(5); }
        }
        private void BuildCommand()
        {
            API.RegisterCommand("PortableMDT", new Action(() =>
            {
                //Check if MDT is already open
                if(API.IsEntityPlayingAnim(Game.PlayerPed.Handle, _animDictionary, _animName, 3)) { return; }

                //Check if player is Dead
                if(Game.PlayerPed.IsDead) { return; }

                //Check duty status -- If play is off duty they cannot open the MDT
                if(!Utilities.IsPlayerOnDuty()) { ShowNotification("~r~Must be on Duty"); return; }

                //Check to make sure the player is not driving
                if(Game.PlayerPed.IsSittingInVehicle())
                {
                    if(API.GetEntitySpeed(Game.PlayerPed.Handle) > 0)
                    {
                        ShowNotification("~r~Can't use MDT while Driving");
                        return;
                    }
                }

                //Create the tablet -- Make sure the network knows it exists
                _prop = API.CreateObject(API.GetHashKey(_propName), 0.5f, 0.5f, 0.5f, true, true, true);
                API.NetworkRegisterEntityAsNetworked(_prop);

                //Attach Tablet to Player
                API.AttachEntityToEntity(_prop, Game.PlayerPed.Handle, API.GetPedBoneIndex(Game.PlayerPed.Handle, 28422), -0.05f, 0f, 0f, 0f, 0f, 0f, true, true, false, true, 1, true);

                //Play Animation
                Game.PlayerPed.Task.PlayAnimation(_animDictionary, _animName, 8.0f, -1, (AnimationFlags)_animFlag);

                //Load Display
                API.SendNuiMessage("{\"type\":\"FIVEPD::Computer::UI\",\"display\":true}");
                API.SetNuiFocus(true, true);

            }), false);
            API.RegisterKeyMapping("PortableMDT", "Portable MDT", "KEYBOARD", "F4");
        }
        private void AddEventHandlers()
        {
            EventHandlers["__cfx_nui:exitComputerMenu"] += new Action(ExitMDT);
        }
        private void ExitMDT()
        {
            Game.PlayerPed.Task.ClearAnimation(_animDictionary, _animName);
            API.SetModelAsNoLongerNeeded((uint)API.GetHashKey(_propName));
            API.DeleteEntity(ref _prop);
        }
        private void ShowNotification(string msg)
        {
            API.SetNotificationTextEntry("STRING");
            API.AddTextComponentString(msg);
            API.DrawNotification(true, true);
        }
    }
}
