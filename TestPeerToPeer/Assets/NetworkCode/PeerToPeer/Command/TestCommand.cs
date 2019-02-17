using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCommand : Command
{

    public static TestCommand StaticCreate()
    {
        TestCommand cmd = new TestCommand();
        cmd.SetPlayerID(NetworkManager_v2.mInstance.GetPlayerID());
        return cmd;
    }

    public override void ProcessCommand()
    {
        //base.ProcessCommand();
        Debug.Log("Test Command");
    }
}
