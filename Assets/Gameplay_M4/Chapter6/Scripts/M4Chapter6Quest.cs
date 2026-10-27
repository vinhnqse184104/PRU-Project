using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PRU.M4
{
    public sealed class M4Chapter6Quest : MonoBehaviour
    {
        public enum QuestState { ClearGuards, SolveRunes, FindPrincess, ReturnToRope, Completed, Lost }
        public M4Chapter3Player player;
        public M4Chapter3Boss[] guardians;
        public M4CaveInteractable[] runes;
        public M4CaveInteractable princess, rope;
        public GameObject gate, princessOnGround, carriedPrincess;
        public bool useBuiltInInput = true;
        public float interactionRange = 2.6f;
        public UnityEvent onGateOpened = new UnityEvent(), onPrincessFound = new UnityEvent(), onChapterCompleted = new UnityEvent();
        public QuestState State { get; private set; }
        public int RunesActivated { get; private set; }
        public int GuardiansDefeated { get; private set; }
        public string Notice { get; private set; } = "Tiến vào hang. Hạ hai linh thạch canh giữ rồi kiểm tra ba ấn bên cổng.";
        public bool IsActive => State != QuestState.Completed && State != QuestState.Lost;
        public Transform CurrentTarget
        {
            get
            {
                if (State == QuestState.ClearGuards && guardians != null)
                { foreach(var guard in guardians) if(guard != null && guard.health != null && guard.health.IsAlive) return guard.transform; }
                if (State == QuestState.SolveRunes && runes != null && RunesActivated < runes.Length) return runes[RunesActivated].transform;
                return State == QuestState.FindPrincess ? princess?.transform : rope?.transform;
            }
        }
        int lastInteractionFrame = -1;
        float nextInteraction;
        public static bool SavedCompleted => PlayerPrefs.GetInt("PRU_M4_Chapter6Completed", 0) == 1;
        public static void ClearSavedProgress() { PlayerPrefs.DeleteKey("PRU_M4_Chapter6Completed"); PlayerPrefs.Save(); }
        void Start()
        {
            State = QuestState.ClearGuards; RunesActivated = GuardiansDefeated = 0;
            lastInteractionFrame = -1; nextInteraction = 0;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (gate != null) gate.SetActive(true);
            if (princessOnGround != null) princessOnGround.SetActive(true);
            if (carriedPrincess != null) carriedPrincess.SetActive(false);
            if (player != null && carriedPrincess != null)
            {
                var pose=player.GetComponent<M4CaveCarryPose>() ?? player.gameObject.AddComponent<M4CaveCarryPose>();
                pose.characterAnimator=player.characterAnimator;pose.carriedVisual=carriedPrincess.transform;
            }
            RefreshRunes();
        }
        void Update()
        {
            if (player == null || player.health == null) return;
            if (IsActive && !player.health.IsAlive) Finish(QuestState.Lost);
            if (State == QuestState.ClearGuards)
            {
                GuardiansDefeated = 0;
                if (guardians != null) foreach(var guard in guardians) if(guard != null && guard.health != null && !guard.health.IsAlive) GuardiansDefeated++;
                if (guardians != null && guardians.Length > 0 && GuardiansDefeated == guardians.Length)
                {
                    State = QuestState.SolveRunes;
                    Notice = "Hai linh thạch đã bị hạ. Bảng đá ghi: MẶT TRỜI → TRĂNG → SAO. Đến từng ấn và bấm E theo thứ tự.";
                }
            }
            if (player.transform.position.y < -3 && IsActive)
                player.health.TakeDamage(player.health.CurrentHealth, gameObject);
            if (!useBuiltInInput || Keyboard.current == null) return;
            if (Keyboard.current.rKey.wasPressedThisFrame) RestartChapter();
            else if (Keyboard.current.escapeKey.wasPressedThisFrame) BackToMenu();
            else if (IsActive && Keyboard.current.eKey.wasPressedThisFrame)
            {
                M4CaveInteractable closest = null; float distance = float.PositiveInfinity;
                foreach(var target in FindObjectsByType<M4CaveInteractable>(FindObjectsSortMode.None))
                    if(target.quest == this && CanReach(target) && Vector3.Distance(player.transform.position,target.transform.position)<distance)
                    { closest=target;distance=Vector3.Distance(player.transform.position,target.transform.position); }
                if(closest!=null)TryInteract(closest);
            }
            else if (State == QuestState.Completed && Keyboard.current.enterKey.wasPressedThisFrame) BackToMenu();
        }
        public bool CanReach(M4CaveInteractable target)
        {
            if (!IsActive || player == null || target == null || target.quest != this || !target.isActiveAndEnabled) return false;
            Vector3 d = target.transform.position - player.transform.position;
            if(Mathf.Abs(d.y)>2.5f || new Vector2(d.x,d.z).magnitude>interactionRange)return false;
            Vector3 origin=player.transform.position+Vector3.up, endpoint=target.transform.position+Vector3.up;
            foreach(var hit in Physics.RaycastAll(origin,endpoint-origin,Vector3.Distance(origin,endpoint),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(player.transform)&&!hit.transform.IsChildOf(target.transform))return false;
            return true;
        }
        public bool TryInteract(M4CaveInteractable target)
        {
            if (!CanReach(target) || lastInteractionFrame == Time.frameCount || Time.time < nextInteraction) return false;
            lastInteractionFrame = Time.frameCount; nextInteraction = Time.time + .25f;
            if(State == QuestState.ClearGuards){Notice="Hãy hạ các linh thạch canh hang trước khi giải ấn.";return false;}
            if(State == QuestState.SolveRunes)
            {
                int index=Array.IndexOf(runes,target);
                if(index<0)return false;
                if(index!=RunesActivated){RunesActivated=0;RefreshRunes();Notice="Sai thứ tự! Các ấn đã tắt. Bấm lại: MẶT TRỜI → TRĂNG → SAO.";return false;}
                RunesActivated++;RefreshRunes();
                if(RunesActivated==runes.Length)
                {
                    if(gate!=null)gate.SetActive(false);
                    State=QuestState.FindPrincess;Notice="Cổng đá đã mở. Đi vào sâu, đến gần Công Chúa và bấm E để giúp nàng.";
                    onGateOpened.Invoke();
                }
                else Notice="Đã sáng "+RunesActivated+"/"+runes.Length+" ấn. Tiếp tục theo thứ tự khắc trên bảng đá.";
                return true;
            }
            if(State == QuestState.FindPrincess && target==princess)
            {
                State=QuestState.ReturnToRope;
                player.SetAttackingEnabled(false);
                if(princessOnGround!=null)princessOnGround.SetActive(false);
                if(carriedPrincess!=null)carriedPrincess.SetActive(true);
                Notice="Đã tìm thấy Công Chúa! Đưa nàng về điểm dây thừng, chú ý bẫy trên đường quay lại.";
                onPrincessFound.Invoke();return true;
            }
            if(State == QuestState.ReturnToRope && target==rope)
            {
                PlayerPrefs.SetInt("PRU_M4_Chapter6Completed",1);PlayerPrefs.Save();
                M4StoryFlow.CompleteStory();
                Finish(QuestState.Completed);onChapterCompleted.Invoke();return true;
            }
            return false;
        }
        void RefreshRunes()
        {if(runes!=null)for(int i=0;i<runes.Length;i++)if(runes[i]!=null)runes[i].SetLit(i<RunesActivated);}
        void Finish(QuestState state)
        {
            State=state;player.SetInputLocked(true);player.health.SetDamageEnabled(false);
            if(guardians!=null)foreach(var guard in guardians)if(guard!=null)guard.SetCombatPaused(true);
            Notice=state==QuestState.Completed?"Đã đưa Công Chúa đến dây thừng. Chương 6 hoàn thành! Chưa chạy đoạn phản bội chương 7.":"Thạch Sanh đã gục ngã. Bấm R để thử lại từ cửa hang.";
        }
        public void RestartChapter()
        {
            if(!Application.CanStreamedLevelBeLoaded(gameObject.scene.path))return;
            ClearSavedProgress();SceneManager.LoadScene(gameObject.scene.path);
        }
        public void BackToMenu()
        {if(Application.CanStreamedLevelBeLoaded(M4StoryFlow.MenuScene))SceneManager.LoadScene(M4StoryFlow.MenuScene);}
        void OnGUI()
        {
            string objective=State switch {
                QuestState.ClearGuards=>"Hạ hai linh thạch canh hang. Né khi chúng báo đòn.",
                QuestState.SolveRunes=>"Giải ấn: MẶT TRỜI → TRĂNG → SAO. Đến gần và bấm E.",
                QuestState.FindPrincess=>"Qua cổng đã mở, tìm và giúp Công Chúa bằng E.",
                QuestState.ReturnToRope=>"Đưa Công Chúa về điểm dây thừng và bấm E.",
                QuestState.Completed=>"Đã cứu Công Chúa và tìm lại lối lên.",_=>"Bấm R để chơi lại chương 6."};
            M4QuestDrawing.Objective("CHƯƠNG 6 · HANG ĐẠI BÀNG",objective,"Canh hang: "+GuardiansDefeated+"/"+(guardians!=null?guardians.Length:0)+"  •  Ấn sáng: "+RunesActivated+"/3",
                "WASD: đi  •  Shift: chạy  •  Space: nhảy  •  Giữ chuột phải: xoay camera  •  Chuột trái: rìu  •  E: tương tác  •  R: thử lại  •  Esc: menu");
            if(IsActive && player!=null && CurrentTarget!=null)
                M4QuestDrawing.Target(player.viewCamera,CurrentTarget.position,player.transform.position,State==QuestState.ClearGuards?"CANH HANG":State==QuestState.SolveRunes?runes[RunesActivated].label:State==QuestState.FindPrincess?"CÔNG CHÚA":"DÂY THỪNG");
            if(player!=null && player.health!=null)
            {
                var style=M4QuestDrawing.Text(18,Color.white,true);
                GUI.Label(new Rect(Screen.width-250,25,230,30),"THẠCH SANH  "+Mathf.CeilToInt(player.health.CurrentHealth)+" / "+player.health.maxHealth,style);
            }
            float width=Mathf.Min(850,Screen.width-40);
            var box=new Rect((Screen.width-width)/2,Screen.height-142,width,70);
            M4QuestDrawing.Panel(box,new Color(.02f,.05f,.055f,.95f));
            GUI.Label(new Rect(box.x+15,box.y+10,width-30,56),Notice,M4QuestDrawing.Text(17,Color.white));
            if(!IsActive)
            {
                var center=new Rect((Screen.width-620)/2,(Screen.height-240)/2,620,240);
                M4QuestDrawing.Panel(center,new Color(.02f,.05f,.055f,.99f));
                GUI.Label(new Rect(center.x+25,center.y+25,570,55),State==QuestState.Completed?"CHƯƠNG 6 HOÀN THÀNH":"THẠCH SANH ĐÃ GỤC NGÃ",M4QuestDrawing.Text(26,M4QuestDrawing.Gold,true));
                GUI.Label(new Rect(center.x+25,center.y+90,570,75),Notice,M4QuestDrawing.Text(19,Color.white));
                if(GUI.Button(new Rect(center.x+25,center.y+180,260,40),"CHƠI LẠI [R]"))RestartChapter();
                if(GUI.Button(new Rect(center.x+310,center.y+180,285,40),"VỀ MENU"))BackToMenu();
            }
        }
    }
}
