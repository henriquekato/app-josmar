using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class UpdateKeyStatusButton : MonoBehaviour
{
    [SerializeField] Dropdown dpdRequestsList;
    [SerializeField] Text txtStatus;
    [SerializeField] Button btnReturn;
    [SerializeField] Button btnStart;
    [SerializeField] Button btnCancel;
    [SerializeField] Button btnReturnKey;
    [SerializeField] Button btnClose;
    [SerializeField] GameObject panelMsg;
    [SerializeField] Text txtMsg;

    private RequestUpdateJson jsonRequestUpdate;
    private RequestGetJson jsonRequestGetStarted;
    private RequestGetJson jsonRequestGetEnded;

    public void UpdateKeyStatusToStart()
    {
        Utilities.StartRequest(new Button[] {btnReturn, btnStart, btnCancel, btnReturnKey, btnClose}, txtMsg, "Carregando...", panelMsg);

        Key key = Utilities.WhichRequest(dpdRequestsList);
        
        if(VerifyTime.TimeOk(key))
        {
            StartCoroutine(PostUpdateKeyStatus(key, (int)Utilities.Status.start_request));
        }
        else
        {
            Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "A hora do seu pedido ainda não chegou", PanelMsg:panelMsg, Connection:true, _Key:key);
            return;
        }
    }

    public void UpdateKeyStatusToEnded()
    {
        Utilities.StartRequest(new Button[] {btnReturn, btnStart, btnCancel, btnReturnKey, btnClose}, txtMsg, "Carregando...", panelMsg);
        Key key = Utilities.WhichRequest(dpdRequestsList);
        StartCoroutine(PostUpdateKeyStatus(key, (int)Utilities.Status.end_request));
    }

    public void UpdateKeyStatusToCancel()
    {
        Utilities.StartRequest(new Button[] {btnReturn, btnStart, btnCancel, btnReturnKey, btnClose}, txtMsg, "Carregando...", panelMsg);
        Key key = Utilities.WhichRequest(dpdRequestsList);
        StartCoroutine(PostUpdateKeyStatus(key, (int)Utilities.Status.canceled));
    }

    private IEnumerator PostUpdateKeyStatus(Key key, int IStatus)
    {
        string sStatus = "";
        switch(IStatus)
        {
            case (int)Utilities.Status.start_request:
                sStatus = "start_request";
                break;
            case (int)Utilities.Status.end_request:
                sStatus = "end_request";
                break;
            case (int)Utilities.Status.canceled:
                sStatus = "canceled";
                break;
        }

        WWWForm form = new WWWForm();
        form.AddField("id", key.requestId.ToString());
        form.AddField("status", sStatus);
        form.AddField("token", User.user.UserToken);
        
        UnityWebRequest requestRequestUpdate = UnityWebRequest.Post(Utilities.apiURL + Utilities.requestUpdateStatusURL, form);
        requestRequestUpdate.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        yield return requestRequestUpdate.SendWebRequest();

        if(requestRequestUpdate.result == UnityWebRequest.Result.ConnectionError | requestRequestUpdate.result == UnityWebRequest.Result.ProtocolError)
        {
            Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Erro de conexão", PanelMsg:panelMsg);
        }
        else
        {
            jsonRequestUpdate = JsonUtility.FromJson<RequestUpdateJson>(requestRequestUpdate.downloadHandler.text);

            switch(jsonRequestUpdate.code)
            {
                case "request_error_on_update_status":
                    Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Erro ao atualizar status do pedido", PanelMsg:panelMsg, Connection: true, _Key:key);
                    break;
                case "request_updated_status":
                    switch(IStatus)
                    {
                        case (int)Utilities.Status.start_request:
                            txtMsg.text = "Liberando chave...";
                            StartCoroutine(GetStartedKeyStatus(key, IStatus));
                            break;
                        case (int)Utilities.Status.end_request:
                            txtMsg.text = "Devolvendo chave...";
                            StartCoroutine(GetEndedKeyStatus(key, IStatus));
                            break;
                        case (int)Utilities.Status.canceled:
                            User.user.UserKeys.Remove(key);

                            Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Pedido cancelado com sucesso", TxtStatus:txtStatus, PanelMsg:panelMsg, Connection:true, Success:true, Status:IStatus, _Key:key);
                            break;
                    }
                    break;
                default:
                    Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Erro inesperado: " + jsonRequestUpdate.code, TxtStatus:txtStatus, PanelMsg:panelMsg, Connection:true);
                    break;
            }
        }
    }

    private IEnumerator GetStartedKeyStatus(Key key, int IStatus)
    {
        UnityWebRequest requestGetKeyStatus = UnityWebRequest.Get(Utilities.apiURL + Utilities.requestGetURL + "?id=" + key.requestId.ToString() + "&token=" + User.user.UserToken);
        yield return requestGetKeyStatus.SendWebRequest();

        if(requestGetKeyStatus.result == UnityWebRequest.Result.ConnectionError | requestGetKeyStatus.result == UnityWebRequest.Result.ProtocolError)
        {
            Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Erro de conexão", PanelMsg:panelMsg);
        }
        else
        {
            jsonRequestGetStarted = JsonUtility.FromJson<RequestGetJson>(requestGetKeyStatus.downloadHandler.text);

            switch(jsonRequestGetStarted.code)
            {
                case "request_got":
                    if(jsonRequestGetStarted.request.status == "started")
                    {
                        int i = User.user.UserKeys.IndexOf(key);
                        User.user.UserKeys[i].status = (int)Utilities.Status.started;

                        Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Chave liberada com sucesso", TxtStatus:txtStatus, PanelMsg:panelMsg, Connection:true, Success:true, Status:IStatus, _Key:key);
                    }
                    else
                    {
                        yield return new WaitForSeconds(5);
                        StartCoroutine(GetStartedKeyStatus(key, IStatus));
                    }
                    break;
                default:
                    Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Erro inesperado: " + jsonRequestGetStarted.code, TxtStatus:txtStatus, PanelMsg:panelMsg, Connection:true);
                    break;
            }
        }
    }

    private IEnumerator GetEndedKeyStatus(Key key, int IStatus)
    {
        UnityWebRequest requestGetKeyStatus = UnityWebRequest.Get(Utilities.apiURL + Utilities.requestGetURL + "?id=" + key.requestId.ToString() + "&token=" + User.user.UserToken);
        yield return requestGetKeyStatus.SendWebRequest();

        if(requestGetKeyStatus.result == UnityWebRequest.Result.ConnectionError | requestGetKeyStatus.result == UnityWebRequest.Result.ProtocolError)
        {
            Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Erro de conexão", PanelMsg:panelMsg);
        }
        else
        {
            jsonRequestGetEnded = JsonUtility.FromJson<RequestGetJson>(requestGetKeyStatus.downloadHandler.text);

            switch(jsonRequestGetEnded.code)
            {
                case "request_got":
                    if(jsonRequestGetEnded.request.status == "ended")
                    {
                        User.user.UserKeys.Remove(key);

                        Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Chave devolvida com sucesso", TxtStatus:txtStatus, PanelMsg:panelMsg, Connection:true, Success:true, Status:IStatus, _Key:key);
                    }
                    else
                    {
                        yield return new WaitForSeconds(5);
                        StartCoroutine(GetEndedKeyStatus(key, IStatus));
                    }
                    break;
                default:
                    Utilities.EndUpdateRequest(btnReturn, btnStart, btnCancel, btnReturnKey, btnClose, txtMsg, "Erro inesperado: " + jsonRequestGetEnded.code, TxtStatus:txtStatus, PanelMsg:panelMsg, Connection:true);
                    break;
            }
        }
    }
}
