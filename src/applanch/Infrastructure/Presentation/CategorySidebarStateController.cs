namespace applanch.Infrastructure.Presentation;

internal sealed class CategorySidebarStateController
{
    private bool _isPointerOverSidebar;
    private bool _isPointerOverTrigger;

    internal bool IsPinned { get; private set; } = true;

    internal bool IsExpanded { get; private set; } = true;

    internal bool SetPinned(bool isPinned)
    {
        if (IsPinned == isPinned)
        {
            return false;
        }

        IsPinned = isPinned;
        SetExpanded(isPinned || _isPointerOverSidebar || _isPointerOverTrigger);
        return true;
    }

    internal bool HandleSidebarEntered()
    {
        _isPointerOverSidebar = true;
        return SetExpanded(true);
    }

    internal void HandleSidebarExited()
    {
        _isPointerOverSidebar = false;
    }

    internal bool HandleTriggerEntered()
    {
        _isPointerOverTrigger = true;

        if (IsPinned)
        {
            return false;
        }

        return SetExpanded(true);
    }

    internal void HandleTriggerExited()
    {
        _isPointerOverTrigger = false;
    }

    internal bool TryCollapse()
    {
        if (IsPinned || _isPointerOverSidebar || _isPointerOverTrigger)
        {
            return false;
        }

        return SetExpanded(false);
    }

    private bool SetExpanded(bool isExpanded)
    {
        if (IsExpanded == isExpanded)
        {
            return false;
        }

        IsExpanded = isExpanded;
        return true;
    }
}