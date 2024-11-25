using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text;

public class CubeEvent : MonoBehaviour, IPointerClickHandler
{
    // �}�C�N�̊J�n�E�I���Ǘ�
    bool flagMicRecordStart = false;

    // �}�C�N�f�o�C�X���L���b�`�ł������ǂ���
    bool catchedMicDevice = false;

    // ���ݘ^������}�C�N�f�o�C�X��
    string currentRecordingMicDeviceName = "null";

    // PC �̘^���̃^�[�Q�b�g�ɂȂ�}�C�N�f�o�C�X��
    // ����͂��g���̃f�o�C�X�ŕς��܂�
    // ���S��v�łȂ��Ǝ󂯎��Ȃ��̂Œ���
    string recordingTargetMicDeviceName = "Krisp Microphone (Krisp Audio)";

    // �w�b�_�[�T�C�Y
    int HeaderByteSize = 44;

    // BitsPerSample
    int BitsPerSample = 16;

    // AudioFormat
    int AudioFormat = 1;

    // �^������ AudioClip
    AudioClip recordedAudioClip;

    // �T���v�����O���g��
    int samplingFrequency = 44100;

    // �ő�^������[sec]
    int maxTimeSeconds = 10;

    // Wav �f�[�^
    byte[] dataWav;

    // OpenAIAPIKey
    // WhisperAPI �� ChatGPTAPI �ŋ���
    string OpenAIAPIKey = "OpenAIAPIKey";

    // Wisper API �Ŏ�M���� JSON �f�[�^�� Unity �ň����f�[�^�ɂ��� WhisperAPIResponseData �x�[�X�N���X
    [Serializable]
    public class WhisperAPIResponseData
    {
        public string text;
    }

    // ChatGPT API �Ŏ�M���� JSON �f�[�^�� Unity �ň����f�[�^�ɂ��� ResponseData �x�[�X�N���X
    // API�d�l : https://platform.openai.com/docs/api-reference/completions/object
    [Serializable]
    public class ResponseData
    {
        public string id;
        public string @object; // object �͗\���Ȃ̂� @ ���g���ăG�X�P�[�v���Ă��܂�
        public int created;
        public List<ResponseDataChoice> choices;
        public ResponseDataUsage usage;
    }

    [Serializable]
    public class ResponseDataUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }
    [Serializable]
    public class ResponseDataChoice
    {
        public int index;
        public RequestDataMessages message;
        public string finish_reason;
    }

    // ChatGPT API �ɑ��M���� Unity �f�[�^�� JSON �f�[�^������ RequestData �x�[�X�N���X
    [Serializable]
    public class RequestData
    {
        public string model;
        public List<RequestDataMessages> messages;
    }

    [Serializable]
    public class RequestDataMessages
    {
        public string role;
        public string content;
    }


    void Start()
    {
        catchedMicDevice = false;

        Launch();
    }

    void Launch()
    {

        // �}�C�N�f�o�C�X��T��
        foreach (string device in Microphone.devices)
        {
            Debug.Log($"Mic device name : {device}");

            // PC �p�̃}�C�N�f�o�C�X�����蓖��
            if (device == recordingTargetMicDeviceName)
            {
                Debug.Log($"{recordingTargetMicDeviceName} searched");

                currentRecordingMicDeviceName = device;

                catchedMicDevice = true;
            }

        }

        if (catchedMicDevice)
        {
            Debug.Log($"�}�C�N�{������");
            Debug.Log($"currentRecordingMicDeviceName : {currentRecordingMicDeviceName}");
        }
        else
        {
            Debug.Log($"�}�C�N�{�����s");
        }

    }

    void Update()
    {

    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (catchedMicDevice)
        {
            if (flagMicRecordStart)
            {
                // Stop
                // �}�C�N�̘^�����J�n
                flagMicRecordStart = false;
                Debug.Log($"Mic Record Stop");

                RecordStop();

            }
            else
            {
                // Start
                // �}�C�N�̒�~
                flagMicRecordStart = true;
                Debug.Log($"Mic Record Start");

                RecordStart();
            }
        }

    }

    void RecordStart()
    {
        // �}�C�N�̘^�����J�n���� AudioClip �����蓖��
        recordedAudioClip = Microphone.Start(currentRecordingMicDeviceName, false, maxTimeSeconds, samplingFrequency);
    }

    void RecordStop()
    {
        // �}�C�N�̒�~
        Microphone.End(currentRecordingMicDeviceName);

        Debug.Log($"WAV �f�[�^�쐬�J�n");

        // using ���g���ă������J���������ōs��
        using (MemoryStream currentMemoryStream = new MemoryStream())
        {
            // ChunkID RIFF
            byte[] bufRIFF = Encoding.ASCII.GetBytes("RIFF");
            currentMemoryStream.Write(bufRIFF, 0, bufRIFF.Length);

            // ChunkSize
            byte[] bufChunkSize = BitConverter.GetBytes((UInt32)(HeaderByteSize + recordedAudioClip.samples * recordedAudioClip.channels * BitsPerSample / 8));
            currentMemoryStream.Write(bufChunkSize, 0, bufChunkSize.Length);

            // Format WAVE
            byte[] bufFormatWAVE = Encoding.ASCII.GetBytes("WAVE");
            currentMemoryStream.Write(bufFormatWAVE, 0, bufFormatWAVE.Length);

            // Subchunk1ID fmt
            byte[] bufSubchunk1ID = Encoding.ASCII.GetBytes("fmt ");
            currentMemoryStream.Write(bufSubchunk1ID, 0, bufSubchunk1ID.Length);

            // Subchunk1Size (16 for PCM)
            byte[] bufSubchunk1Size = BitConverter.GetBytes((UInt32)16);
            currentMemoryStream.Write(bufSubchunk1Size, 0, bufSubchunk1Size.Length);

            // AudioFormat (PCM=1)
            byte[] bufAudioFormat = BitConverter.GetBytes((UInt16)AudioFormat);
            currentMemoryStream.Write(bufAudioFormat, 0, bufAudioFormat.Length);

            // NumChannels
            byte[] bufNumChannels = BitConverter.GetBytes((UInt16)recordedAudioClip.channels);
            currentMemoryStream.Write(bufNumChannels, 0, bufNumChannels.Length);

            // SampleRate
            byte[] bufSampleRate = BitConverter.GetBytes((UInt32)recordedAudioClip.frequency);
            currentMemoryStream.Write(bufSampleRate, 0, bufSampleRate.Length);

            // ByteRate (=SampleRate * NumChannels * BitsPerSample/8)
            byte[] bufByteRate = BitConverter.GetBytes((UInt32)(recordedAudioClip.samples * recordedAudioClip.channels * BitsPerSample / 8));
            currentMemoryStream.Write(bufByteRate, 0, bufByteRate.Length);

            // BlockAlign (=NumChannels * BitsPerSample/8)
            byte[] bufBlockAlign = BitConverter.GetBytes((UInt16)(recordedAudioClip.channels * BitsPerSample / 8));
            currentMemoryStream.Write(bufBlockAlign, 0, bufBlockAlign.Length);

            // BitsPerSample
            byte[] bufBitsPerSample = BitConverter.GetBytes((UInt16)BitsPerSample);
            currentMemoryStream.Write(bufBitsPerSample, 0, bufBitsPerSample.Length);

            // Subchunk2ID data
            byte[] bufSubchunk2ID = Encoding.ASCII.GetBytes("data");
            currentMemoryStream.Write(bufSubchunk2ID, 0, bufSubchunk2ID.Length);

            // Subchuk2Size
            byte[] bufSubchuk2Size = BitConverter.GetBytes((UInt32)(recordedAudioClip.samples * recordedAudioClip.channels * BitsPerSample / 8));
            currentMemoryStream.Write(bufSubchuk2Size, 0, bufSubchuk2Size.Length);

            // Data
            float[] floatData = new float[recordedAudioClip.samples * recordedAudioClip.channels];
            recordedAudioClip.GetData(floatData, 0);

            foreach (float f in floatData)
            {
                byte[] bufData = BitConverter.GetBytes((short)(f * short.MaxValue));
                currentMemoryStream.Write(bufData, 0, bufData.Length);
            }

            Debug.Log($"WAV �f�[�^�쐬����");

            dataWav = currentMemoryStream.ToArray();

            Debug.Log($"dataWav.Length {dataWav.Length}");

            // �܂� Wisper API �ŕ����N����
            StartCoroutine(PostWhisperAPI());

        }

    }

    // Wisper API �ŕ����N����
    IEnumerator PostWhisperAPI()
    {
        // IMultipartFormSection �� multipart/form-data �̃f�[�^�Ƃ��đ���܂�
        // https://docs.unity3d.com/ja/2018.4/Manual/UnityWebRequest-SendingForm.html
        // https://docs.unity3d.com/ja/2019.4/ScriptReference/Networking.IMultipartFormSection.html
        // https://docs.unity3d.com/ja/2020.3/ScriptReference/Networking.MultipartFormDataSection.html
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        // https://platform.openai.com/docs/api-reference/audio/createTranscription
        // Whisper ���f�����g��
        formData.Add(new MultipartFormDataSection("model", "whisper-1"));
        // ���{��ŕԓ�
        formData.Add(new MultipartFormDataSection("language", "ja"));
        // WAV �f�[�^������
        formData.Add(new MultipartFormFileSection("file", dataWav, "whisper01.wav", "multipart/form-data"));

        // HTTP ���N�G�X�g����(POST ���\�b�h) UnityWebRequest ���Ăяo��
        // �� 2 �����ŏ�L�̃t�H�[���f�[�^�����蓖�Ă� multipart/form-data �̃f�[�^�Ƃ��đ���܂�
        string urlWhisperAPI = "https://api.openai.com/v1/audio/transcriptions";
        UnityWebRequest request = UnityWebRequest.Post(urlWhisperAPI, formData);

        // OpenAI �F�؂� Authorization �w�b�_�[�� Bearer �̂��Ƃ� API �g�[�N��������
        request.SetRequestHeader("Authorization", $"Bearer {OpenAIAPIKey}");

        // �_�E�����[�h�i�T�[�o��Unity�j�̃n���h�����쐬
        request.downloadHandler = new DownloadHandlerBuffer();

        Debug.Log("WhisperAPI ���N�G�X�g�J�n");

        // ���N�G�X�g�J�n
        yield return request.SendWebRequest();


        // ���ʂɂ���ĕ���
        switch (request.result)
        {
            case UnityWebRequest.Result.InProgress:
                Debug.Log("WhisperAPI ���N�G�X�g��");
                break;

            case UnityWebRequest.Result.ProtocolError:
                Debug.Log("ProtocolError");
                Debug.Log(request.responseCode);
                Debug.Log(request.error);
                break;

            case UnityWebRequest.Result.ConnectionError:
                Debug.Log("ConnectionError");
                break;

            case UnityWebRequest.Result.Success:
                Debug.Log("WhisperAPI ���N�G�X�g����");

                // �R���\�[���ɕ\��
                Debug.Log($"responseData: {request.downloadHandler.text}");

                WhisperAPIResponseData resultResponseWhisperAPI = JsonUtility.FromJson<WhisperAPIResponseData>(request.downloadHandler.text);

                // �e�L�X�g���N�������� ChatGPT API �ɕ���
                StartCoroutine(PostChatGPT(resultResponseWhisperAPI.text));

                break;
        }


    }

    // ChatGPT API
    IEnumerator PostChatGPT(string text)
    {
        // HTTP ���N�G�X�g����(POST ���\�b�h) UnityWebRequest ���Ăяo��
        // ���N�G�X�g�d�l : https://platform.openai.com/docs/guides/gpt/chat-completions-api
        // API�d�l : https://platform.openai.com/docs/api-reference/completions/object
        UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");

        RequestData requestData = new RequestData();
        // �f�[�^��ݒ�
        requestData.model = "gpt-4o-mini";
        RequestDataMessages currentMessage = new RequestDataMessages();
        // ���[���� user
        currentMessage.role = "user";
        // ���ۂ̎���
        currentMessage.content = text;
        List<RequestDataMessages> currentMessages = new List<RequestDataMessages>();
        currentMessages.Add(currentMessage);
        requestData.messages = currentMessages;
        Debug.Log($"currentMessages[0].content : {currentMessages[0].content}");

        // ���M�f�[�^�� JsonUtility.ToJson �� JSON ��������쐬
        // RequestData, RequestDataMessages �̍\���Ɋ�Â��ĕϊ����Ă����
        string strJSON = JsonUtility.ToJson(requestData);
        Debug.Log($"strJSON : {strJSON}");
        // ���M�f�[�^�� Encoding.UTF8.GetBytes �� byte �f�[�^��
        byte[] bodyRaw = Encoding.UTF8.GetBytes(strJSON);

        // �A�b�v���[�h�iUnity���T�[�o�j�̃n���h�����쐬
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        // �_�E�����[�h�i�T�[�o��Unity�j�̃n���h�����쐬
        request.downloadHandler = new DownloadHandlerBuffer();

        // JSON �ő���� HTTP �w�b�_�[�Ő錾����
        request.SetRequestHeader("Content-Type", "application/json");
        // ChatGPT �p�̔F�؂�`����ݒ�
        request.SetRequestHeader("Authorization", $"Bearer {OpenAIAPIKey}");

        // ���N�G�X�g�J�n
        yield return request.SendWebRequest();

        Debug.Log("ChatGPT ���N�G�X�g...");

        // ���ʂɂ���ĕ���
        switch (request.result)
        {
            case UnityWebRequest.Result.InProgress:
                Debug.Log("ChatGPT ���N�G�X�g��");
                break;

            case UnityWebRequest.Result.ProtocolError:
                Debug.Log("ProtocolError");
                Debug.Log(request.responseCode);
                Debug.Log(request.error);
                break;

            case UnityWebRequest.Result.ConnectionError:
                Debug.Log("ConnectionError");
                break;

            case UnityWebRequest.Result.Success:
                Debug.Log("ChatGPT ���N�G�X�g����");

                // �R���\�[���ɕ\��
                Debug.Log($"responseData: {request.downloadHandler.text}");

                ResponseData resultResponse = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);

                // �ԓ�
                Debug.Log($"resultResponse.choices[0].message : {resultResponse.choices[0].message.content}");

                break;
        }

        request.Dispose();
    }
}