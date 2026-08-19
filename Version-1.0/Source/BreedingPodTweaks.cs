using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BlockingSystem;
using Timberborn.Buildings;
using Timberborn.Emptying;
using Timberborn.Localization;
using Timberborn.Reproduction;
using Timberborn.StatusSystem;
using Timberborn.TickSystem;

namespace Calloatti.BreedingPodTweaks
{
  public class BreedingPodTweaks : TickableComponent, IAwakableComponent, IFinishedStateListener
  {
    private BreedingPod _breedingPod;
    private BlockableObject _blockableObject;
    private Automatable _automatable;
    private PausableBuilding _pausableBuilding;
    private AutoEmptiableBlocker _autoEmptiableBlocker;
    private StatusToggle _automationPauseStatusToggle;
    private StatusToggle _waitingForProgressStatusToggle;
    private float _targetProgress;
    internal bool _evaluatingLocally;
    private bool _ourBlockActive;
    private bool _emptiableBlocked;

    public BreedingPodTweaks(ILoc loc)
    {
      _automationPauseStatusToggle = StatusToggle.CreatePriorityStatusWithFloatingIcon(
        "PausedByAutomation",
        loc.T("Automation.PausedByAutomation"));
      _waitingForProgressStatusToggle = StatusToggle.CreatePriorityStatusWithFloatingIcon(
        "PausedByAutomation1",
        loc.T("Automation.PausedByAutomation"));
    }

    public void Awake()
    {
      _blockableObject = GetComponent<BlockableObject>();
      _automatable = GetComponent<Automatable>();
      _pausableBuilding = GetComponent<PausableBuilding>();
      _breedingPod = GetComponent<BreedingPod>();
      _autoEmptiableBlocker = GetComponent<AutoEmptiableBlocker>();
      GetComponent<StatusSubject>().RegisterStatus(_automationPauseStatusToggle);
      GetComponent<StatusSubject>().RegisterStatus(_waitingForProgressStatusToggle);
      EnableComponent();
    }

    private void TryGetBreedingPod()
    {
      if (_breedingPod == null)
      {
        _breedingPod = GetComponent<BreedingPod>();
      }
    }

    public void OnEnterFinishedState()
    {
      TryGetBreedingPod();
      EnsureTargetProgressGenerated();
      EnableComponent();
      UpdateAutomationBlock();
    }

    public void OnExitFinishedState()
    {
      _targetProgress = 0f;
      if (_ourBlockActive)
      {
        _evaluatingLocally = true;
        _blockableObject.Unblock(this);
        _evaluatingLocally = false;
        _ourBlockActive = false;
      }
      if (_emptiableBlocked)
      {
        BlockAutoEmptiable(false);
      }
      _automationPauseStatusToggle.Deactivate();
      _waitingForProgressStatusToggle.Deactivate();
      DisableComponent();
    }

    private void EnsureTargetProgressGenerated()
    {
      if (_targetProgress <= 0f)
      {
        _targetProgress = UnityEngine.Random.Range(0.90f, 0.99f);
      }
    }

    private float CalculateProgress()
    {
      if (_breedingPod == null) return 0f;
      return _breedingPod.CalculateProgress();
    }

    private bool IsManualPause()
    {
      return _pausableBuilding != null && _pausableBuilding.Paused;
    }

    private bool IsAutomationRequestingPause()
    {
      if (_automatable == null) return false;
      return _automatable.State == ConnectionState.Off;
    }

    private void BlockAutoEmptiable(bool block)
    {
      if (_autoEmptiableBlocker == null) return;
      if (block && !_emptiableBlocked)
      {
        _autoEmptiableBlocker.IncrementBlockingToggles();
        _emptiableBlocked = true;
      }
      else if (!block && _emptiableBlocked)
      {
        _autoEmptiableBlocker.DecrementBlockingToggles();
        _emptiableBlocked = false;
      }
    }

    private void UpdateAutomationBlock()
    {
      bool manualPause = IsManualPause();
      bool automationWantsPause = IsAutomationRequestingPause();

      EnsureTargetProgressGenerated();
      float currentProgress = CalculateProgress();

      bool reachedTarget = currentProgress >= _targetProgress;

      // Determine final block state
      bool shouldBlock = false;
      bool shouldShowAutomationIcon = false;
      bool shouldShowWaitingIcon = false;

      if (manualPause)
      {
        shouldBlock = true;
        // Manual pause uses PausableBuilding's own icon
      }
      else if (automationWantsPause && reachedTarget)
      {
        shouldBlock = true;
        shouldShowAutomationIcon = true;
      }
      else if (automationWantsPause && !reachedTarget)
      {
        // Automation wants to pause but we haven't reached target yet
        shouldShowWaitingIcon = true;
      }

      // Update block state
      if (shouldBlock && !_ourBlockActive)
      {
        _evaluatingLocally = true;
        _blockableObject.Block(this);
        _evaluatingLocally = false;
        _ourBlockActive = true;
      }
      else if (!shouldBlock && _ourBlockActive)
      {
        _evaluatingLocally = true;
        _blockableObject.Unblock(this);
        _evaluatingLocally = false;
        _ourBlockActive = false;
      }

      // Update automation pause icon
      if (shouldShowAutomationIcon && !_automationPauseStatusToggle.IsActive)
      {
        _automationPauseStatusToggle.Activate();
      }
      else if (!shouldShowAutomationIcon && _automationPauseStatusToggle.IsActive)
      {
        _automationPauseStatusToggle.Deactivate();
      }

      // Update waiting-for-progress icon
      if (shouldShowWaitingIcon && !_waitingForProgressStatusToggle.IsActive)
      {
        _waitingForProgressStatusToggle.Activate();
      }
      else if (!shouldShowWaitingIcon && _waitingForProgressStatusToggle.IsActive)
      {
        _waitingForProgressStatusToggle.Deactivate();
      }

      // Manage AutoEmptiableBlocker — only when automation blocks AND not manually paused
      bool shouldBlockEmptiable = automationWantsPause && reachedTarget && !manualPause;
      if (shouldBlockEmptiable && !_emptiableBlocked)
      {
        BlockAutoEmptiable(true);
      }
      else if (!shouldBlockEmptiable && _emptiableBlocked)
      {
        BlockAutoEmptiable(false);
      }
    }

    public override void Tick()
    {
      TryGetBreedingPod();

      if (_breedingPod == null || _blockableObject == null || _automatable == null)
      {
        return;
      }

      UpdateAutomationBlock();

      // Reset state for the next embryo cycle
      float currentProgress = CalculateProgress();
      if (_targetProgress > 0f && currentProgress < 0.05f)
      {
        _targetProgress = 0f;
      }
    }
  }
}
