using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetworkTrungVI;

public class CommandList {
    private Queue<Command> mCommands = new Queue<Command>();
    public CommandList()
    {

    }
    public void Clear() { mCommands.Clear(); }
    public int GetCount() { return mCommands.Count; }
    public void AddCommand(Command _inData) { mCommands.Enqueue(_inData); }

    public void ProcessCommands(int _inPlayerId)
    {
        foreach (Command child in mCommands)
        {
            if (child.GetPlayerID() == _inPlayerId)
            {
                child.ProcessCommand();
            }
        }
    }

    public void Write(ref OutputMemoryBitStream output)
    {
        int cout = GetCount();
        output.Write(cout);
        foreach (Command command in mCommands)
        {
            command.Write(ref output);
        }
    }

    public void Read(ref InputMemoryBitStream input)
    {
        int cout = 0;
        input.Read(ref cout);
        for (int i = 0; i < cout; i++)
        {
            mCommands.Enqueue(Command.StaticReadAndCreate(ref input));
        }
    }
}
