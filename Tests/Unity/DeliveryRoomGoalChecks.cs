using System;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DeliveryRoomGoalChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static bool Passed { get; private set; }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static void Set(object target,string name,object value){var field=target.GetType().GetField(name,Flags);Require(field!=null,"Missing Inspector setting: "+name);field.SetValue(target,value);}
    static object Call(object target,string name,params object[] args){var method=target.GetType().GetMethods(Flags).SingleOrDefault(x=>x.Name==name&&x.GetParameters().Length==args.Length);Require(method!=null,"Missing behavior: "+name);return method.Invoke(target,args);}
    public static string Run()
    {
        Passed=false;var report=new StringBuilder();var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            Require(typeof(DeliveryPoint).GetField("requiredItems",Flags)!=null,"DeliveryPoint needs an editable requiredItems independent of furniture count");
            Require(typeof(DeliveryPoint).GetField("roomName",Flags)!=null,"DeliveryPoint needs an editable roomName for its objective text");
            Require(typeof(DeliveryObjectiveManager).GetField("roomDeliveryPoints",Flags)!=null,"Objective manager needs explicit room goals");
            Func<string,GameObject> create=name=>{var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;};
            var carry=create("Carry fixture").AddComponent<PlayerCarrySystem>();var manager=create("Manager fixture").AddComponent<DeliveryObjectiveManager>();
            Func<string,int,int,DeliveryPoint> point=(name,required,visuals)=>{var p=create(name).AddComponent<DeliveryPoint>();Set(p,"roomName",name);Set(p,"requiredItems",required);Set(p,"objectsToActivate",Enumerable.Range(0,visuals).Select(i=>create(name+" visual "+i)).ToArray());Set(p,"playerCarrySystem",carry);Set(p,"objectiveManager",manager);return p;};
            var legacy=point("Legacy",-1,3);Require(legacy.GetCapacity()==3,"Legacy furniture count remains the default");report.AppendLine("PASS legacy scenes keep their previous capacity");
            var kitchen=point("Kitchen",4,4);var bedroom=point("Bedroom",1,2);var bathroom=point("Bathroom",1,1);var unused=point("Unused",0,1);
            Set(manager,"roomDeliveryPoints",new[]{kitchen,bedroom,bathroom,unused,kitchen,null});Call(manager,"Start");Call(kitchen,"Start");Call(bedroom,"Start");Call(bathroom,"Start");Call(unused,"Start");
            Require(manager.GetTotalItems()==6,"Explicit goals sum kitchen4 + bedroom1 + bathroom1, without duplicate/null/disabled goals");Require(!unused.gameObject.activeSelf,"A room requiring zero items has no usable delivery point");
            var unusedVisual=(GameObject[])typeof(DeliveryPoint).GetField("objectsToActivate",Flags).GetValue(unused);Require(unusedVisual.All(x=>x.activeSelf),"Disabling a delivery task leaves that room furnished");report.AppendLine("PASS zero-target room disabled, furniture retained, duplicate/null goals ignored");
            var text=create("HUD text").AddComponent<TextMeshProUGUI>();var ui=create("HUD fixture").AddComponent<DeliveryObjectiveUI>();Set(ui,"deliveryText",text);Set(ui,"objectiveManager",manager);Set(ui,"showRoomProgress",true);Call(ui,"UpdateUI");Require(text.text=="Kitchen  0 / 4\nBedroom  0 / 1\nBathroom  0 / 1","HUD lists separate room names and required counts");
            for(int i=1;i<=4;i++){Require(carry.TryPickup("Box"),"Pick up kitchen box");kitchen.Interact();Require(!carry.HasItem()&&manager.GetDeliveredItems()==i,"Kitchen delivery counted once");}
            Require(!manager.IsComplete()&&!kitchen.gameObject.activeSelf,"Kitchen closes at four but level waits for the other rooms");Call(ui,"UpdateUI");Require(text.text=="Kitchen  4 / 4\nBedroom  0 / 1\nBathroom  0 / 1","Each room updates its own HUD count");Require(carry.TryPickup("Box"),"Pick up bedroom box");kitchen.Interact();Require(carry.HasItem()&&manager.GetDeliveredItems()==4,"Full kitchen cannot consume bedroom box");bedroom.Interact();Require(!carry.HasItem()&&manager.GetDeliveredItems()==5&&!manager.IsComplete(),"Bedroom delivery still waits for bathroom");Require(carry.TryPickup("Box"),"Pick up bathroom box");bathroom.Interact();Require(!carry.HasItem()&&manager.GetDeliveredItems()==6&&manager.IsComplete(),"Bathroom delivery finishes all room goals");
            var bedroomVisuals=(GameObject[])typeof(DeliveryPoint).GetField("objectsToActivate",Flags).GetValue(bedroom);Require(bedroomVisuals.All(x=>x.activeSelf),"Configured count is independent of the number of final furniture visuals");report.AppendLine("PASS kitchen4 + bedroom1 + bathroom1 => complete; extra kitchen delivery rejected without consuming item; bedroom furniture complete");
            var overflow=point("Custom room",3,1);Set(overflow,"objectiveManager",null);Call(overflow,"Start");for(int i=0;i<3;i++){Require(carry.TryPickup("Box"),"Pick up custom room box");overflow.Interact();}Require(overflow.GetDeliveredCount()==3&&!overflow.gameObject.activeSelf,"Required count greater than visual count is accepted safely");report.AppendLine("PASS custom count larger than furniture list is safe");
            Passed=true;
        }
        catch(Exception error){report.AppendLine("FAIL "+(error is TargetInvocationException&&error.InnerException!=null?error.InnerException:error));}
        finally{foreach(var root in scene.GetRootGameObjects())if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        return report.ToString();
    }
}
