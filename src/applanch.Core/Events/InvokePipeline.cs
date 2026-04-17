namespace applanch.Events;

public delegate void InvokePipeline<TPayload>(AppEvent appEvent, TPayload payload, Action<TPayload> next);

public delegate void InvokePipeline(AppEvent appEvent, Action next);
