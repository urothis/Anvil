using System;
using System.Collections.Generic;
using Anvil.API;
using NWN.Native.API;

namespace Anvil.Services
{
  [ServiceBinding(typeof(DialogService))]
  [ServiceBindingOptions(Lazy = true)]
  public sealed unsafe class DialogService : IDisposable
  {
    private readonly FunctionHook<CheckScriptHook> checkScriptHook;

    private readonly FunctionHook<GetStartEntryHook> getStartEntryHook;
    private readonly FunctionHook<GetStartEntryOneLinerHook> getStartEntryOneLinerHook;
    private readonly FunctionHook<HandleReplyHook> handleReplyHook;
    private readonly FunctionHook<RunScriptHook> runScriptHook;
    private readonly FunctionHook<SendDialogEntryHook> sendDialogEntryHook;
    private readonly FunctionHook<SendDialogRepliesHook> sendDialogRepliesHook;
    private readonly Stack<DialogState> stateStack = new Stack<DialogState>();

    private CNWSDialog? dialog;

    private uint indexEntry;
    private uint indexReply;

    public DialogService(HookService hookService)
    {
      getStartEntryHook = hookService.RequestHook<GetStartEntryHook>(OnGetStartEntry, FunctionsLinux._ZN10CNWSDialog13GetStartEntryEP10CNWSObject, HookOrder.Early);
      getStartEntryOneLinerHook = hookService.RequestHook<GetStartEntryOneLinerHook>(OnGetStartEntryOneLiner, FunctionsLinux._ZN10CNWSDialog21GetStartEntryOneLinerEP10CNWSObjectR13CExoLocStringR7CResRefS5_R13CExoArrayListI11ScriptParamE, HookOrder.Early);
      sendDialogEntryHook = hookService.RequestHook<SendDialogEntryHook>(OnSendDialogEntry, FunctionsLinux._ZN10CNWSDialog15SendDialogEntryEP10CNWSObjectjji, HookOrder.Early);
      sendDialogRepliesHook = hookService.RequestHook<SendDialogRepliesHook>(OnSendDialogReplies, FunctionsLinux._ZN10CNWSDialog17SendDialogRepliesEP10CNWSObjectj, HookOrder.Early);
      handleReplyHook = hookService.RequestHook<HandleReplyHook>(OnHandleReply, FunctionsLinux._ZN10CNWSDialog11HandleReplyEjP10CNWSObjectjij, HookOrder.Early);
      checkScriptHook = hookService.RequestHook<CheckScriptHook>(OnCheckScript, FunctionsLinux._ZN10CNWSDialog11CheckScriptEP10CNWSObjectRK7CResRefRK13CExoArrayListI11ScriptParamE, HookOrder.Early);
      runScriptHook = hookService.RequestHook<RunScriptHook>(OnRunScript, FunctionsLinux._ZN10CNWSDialog9RunScriptEP10CNWSObjectRK7CResRefRK13CExoArrayListI11ScriptParamE, HookOrder.Early);
    }

    private delegate int CheckScriptHook(void* pDialog, void* pNWSObjectOwner, void* sActive, void* scriptParams);

    private delegate uint GetStartEntryHook(void* pDialog, void* pNWSObjectOwner);

    private delegate int GetStartEntryOneLinerHook(void* pDialog, void* pNWSObjectOwner, void* sOneLiner, void* sSound, void* sScript, void* scriptParams);

    private delegate int HandleReplyHook(void* pDialog, uint nPlayerID, void* pNWSObjectOwner, uint nReplyIndex, int bEscapeDialog, uint currentEntryIndex);

    private delegate void RunScriptHook(void* pDialog, void* pNWSObjectOwner, void* sScript, void* scriptParams);

    private delegate int SendDialogEntryHook(void* pDialog, void* pNWSObjectOwner, uint nPlayerIdGUIOnly, uint iEntry, int bPlayHelloSound);

    private delegate int SendDialogRepliesHook(void* pDialog, void* pNWSObjectOwner, uint nPlayerIdGUIOnly);

    public uint? CurrentNodeId
    {
      get
      {
        if (dialog == null)
        {
          return null;
        }

        switch (stateStack.Peek())
        {
          case DialogState.Start:
            return dialog.m_pStartingEntries.ToArray()[CurrentNodeIndex].m_nIndex;
          case DialogState.SendEntry:
            return indexEntry;
          case DialogState.HandleReply:
            return indexReply == 0xFFFFFFFF ? null : dialog.m_pEntries.ToArray()[(int)indexEntry].m_pReplies.ToArray()[(int)indexReply].m_nIndex;
          case DialogState.SendReplies:
            return dialog.m_pEntries.ToArray()[(int)dialog.m_currentEntryIndex].m_pReplies.ToArray()[CurrentNodeIndex].m_nIndex;
          default:
            return null;
        }
      }
    }

    public int CurrentNodeIndex { get; private set; }

    public NodeType CurrentNodeType
    {
      get
      {
        switch (stateStack.Peek())
        {
          case DialogState.Start:
            return NodeType.StartingNode;
          case DialogState.SendEntry:
            return NodeType.EntryNode;
          case DialogState.SendReplies:
          case DialogState.HandleReply:
            return NodeType.ReplyNode;
          default:
            return NodeType.Invalid;
        }
      }
    }

    public ScriptType CurrentScriptType { get; private set; }

    public string? GetCurrentNodeText(Language language = Language.English, Gender gender = Gender.Male)
    {
      CExoLocString? locString = GetCurrentNodeLocString();
      if (locString == null)
      {
        return null;
      }

      CExoString str = new CExoString();
      locString.GetString((int)language, str, (byte)gender, true);
      return str.ToString();
    }

    public void SetCurrentNodeText(string text, Language language = Language.English, Gender gender = Gender.Male)
    {
      CExoLocString? locString = GetCurrentNodeLocString();
      if (locString == null)
      {
        return;
      }

      CExoString exoString = text.ToExoString();
      locString.AddString((int)language, exoString, (byte)gender);
    }

    void IDisposable.Dispose()
    {
      getStartEntryHook.Dispose();
      getStartEntryOneLinerHook.Dispose();
      sendDialogEntryHook.Dispose();
      sendDialogRepliesHook.Dispose();
      handleReplyHook.Dispose();
      checkScriptHook.Dispose();
      runScriptHook.Dispose();
    }

    private CExoLocString? GetCurrentNodeLocString()
    {
      if (dialog == null)
      {
        return null;
      }

      CExoLocString? locString = null;
      switch (stateStack.Peek())
      {
        case DialogState.Start:
          locString = dialog.m_pEntries.ToArray()[(int)dialog.m_pStartingEntries.ToArray()[CurrentNodeIndex].m_nIndex].m_sText;
          break;
        case DialogState.SendEntry:
          locString = dialog.m_pEntries.ToArray()[(int)indexEntry].m_sText;
          break;
        case DialogState.HandleReply:
          uint handleIndex = dialog.m_pEntries.ToArray()[(int)indexEntry].m_pReplies.ToArray()[(int)indexReply].m_nIndex;
          locString = dialog.m_pReplies.ToArray()[(int)handleIndex].m_sText;
          break;
        case DialogState.SendReplies:
          uint sendIndex = dialog.m_pEntries.ToArray()[(int)dialog.m_currentEntryIndex].m_pReplies.ToArray()[CurrentNodeIndex].m_nIndex;
          locString = dialog.m_pReplies.ToArray()[(int)sendIndex].m_sText;
          break;
      }

      return locString;
    }

    private int OnCheckScript(void* pDialog, void* pNWSObjectOwner, void* sActive, void* scriptParams)
    {
      dialog = CNWSDialog.FromPointer(pDialog);

      if (stateStack.Peek() == DialogState.HandleReply)
      {
        stateStack.Pop();
        stateStack.Push(DialogState.SendEntry);

        CNWSDialogEntryArray entries = CNWSDialogEntryArray.FromPointer(dialog.m_pEntries);
        CNWSDialogReplyArray replies = CNWSDialogReplyArray.FromPointer(dialog.m_pReplies);

        indexReply = CNWSDialogLinkReplyArray.FromPointer(entries[(int)indexEntry].m_pReplies)[(int)indexReply].m_nIndex;
        indexEntry = CNWSDialogLinkEntryArray.FromPointer(replies[(int)indexReply].m_pEntries)[CurrentNodeIndex].m_nIndex;
      }

      CurrentScriptType = ScriptType.StartingConditional;
      int retVal = checkScriptHook.CallOriginal(pDialog, pNWSObjectOwner, sActive, scriptParams);
      CurrentNodeIndex++;
      CurrentScriptType = ScriptType.Other;

      return retVal;
    }

    private uint OnGetStartEntry(void* pDialog, void* pNWSObjectOwner)
    {
      dialog = CNWSDialog.FromPointer(pDialog);
      CurrentNodeIndex = 0;

      stateStack.Push(DialogState.Start);
      uint retVal = getStartEntryHook.CallOriginal(pDialog, pNWSObjectOwner);
      stateStack.Pop();

      return retVal;
    }

    private int OnGetStartEntryOneLiner(void* pDialog, void* pNWSObjectOwner, void* sOneLiner, void* sSound, void* sScript, void* scriptParams)
    {
      dialog = CNWSDialog.FromPointer(pDialog);
      CurrentNodeIndex = 0;

      stateStack.Push(DialogState.Start);
      int retVal = getStartEntryOneLinerHook.CallOriginal(pDialog, pNWSObjectOwner, sOneLiner, sSound, sScript, scriptParams);
      stateStack.Pop();

      return retVal;
    }

    private int OnHandleReply(void* pDialog, uint nPlayerID, void* pNWSObjectOwner, uint nReplyIndex, int bEscapeDialog, uint currentEntryIndex)
    {
      dialog = CNWSDialog.FromPointer(pDialog);
      CurrentNodeIndex = 0;

      stateStack.Push(DialogState.HandleReply);
      indexEntry = currentEntryIndex;
      indexReply = nReplyIndex;
      int retVal = handleReplyHook.CallOriginal(pDialog, nPlayerID, pNWSObjectOwner, nReplyIndex, bEscapeDialog, currentEntryIndex);
      stateStack.Pop();

      return retVal;
    }

    private void OnRunScript(void* pDialog, void* pNWSObjectOwner, void* sScript, void* scriptParams)
    {
      dialog = CNWSDialog.FromPointer(pDialog);

      CurrentScriptType = ScriptType.ActionTaken;
      runScriptHook.CallOriginal(pDialog, pNWSObjectOwner, sScript, scriptParams);
      CurrentScriptType = ScriptType.Other;
    }

    private int OnSendDialogEntry(void* pDialog, void* pNWSObjectOwner, uint nPlayerIdGUIOnly, uint iEntry, int bPlayHelloSound)
    {
      dialog = CNWSDialog.FromPointer(pDialog);
      CurrentNodeIndex = 0;

      stateStack.Push(DialogState.SendEntry);
      indexEntry = iEntry;
      int retVal = sendDialogEntryHook.CallOriginal(pDialog, pNWSObjectOwner, nPlayerIdGUIOnly, iEntry, bPlayHelloSound);
      stateStack.Pop();

      return retVal;
    }

    private int OnSendDialogReplies(void* pDialog, void* pNWSObjectOwner, uint nPlayerIdGUIOnly)
    {
      dialog = CNWSDialog.FromPointer(pDialog);
      CurrentNodeIndex = 0;

      stateStack.Push(DialogState.SendReplies);
      int retVal = sendDialogRepliesHook.CallOriginal(pDialog, pNWSObjectOwner, nPlayerIdGUIOnly);
      stateStack.Pop();

      return retVal;
    }
  }
}
