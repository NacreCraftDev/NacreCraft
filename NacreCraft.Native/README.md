# NacreCraft.Native

The native layer that enters the process before any managed code. The managed side sets the `CORECLR_*` profiler variables and restarts, and the runtime loads this library as a profiler at startup; there is no external injector. It carries the stack-overflow guard, the crash path that survives a hardware fault, and the ReJIT executor used by `NacreCraft.Hook`.

## Features

- Early entry — loaded through `CORECLR_ENABLE_PROFILING`, `CORECLR_PROFILER` and `CORECLR_PROFILER_PATH`, which the managed side writes before restarting. `ncn_on_initialize` is the first code the process runs.
- Stack-overflow guard — the vectored exception handler is installed only after the managed side hands over its callback, so it sits in front of the runtime's own handler; the overflow is then reported on a thread that still has stack.
- Early stack check — method bodies are rewritten to call `NativeStackGuard.EnsureStack` at their entry, which turns a deep recursion into a managed exception before the guard page is reached. Framework assemblies are skipped by default and `NCN_REWRITE_MODULES` narrows the set further; `NacreCraft.Util`, which holds the injected entry, is never rewritten. Nothing is touched before `ncn_register_stack_overflow_callback` confirms that assembly is loaded.
- Runtime rewrite — `ncn_request_rewrite` takes a method-body description from `NacreCraft.Hook`, waits for its module to load, asks the runtime for ReJIT and answers the `GetReJITParameters` callback with the new body. Requests are fulfilled on a thread of this module, because entering the profiling API from a managed thread would re-enter the runtime.
- Crash path — `crash` records hardware faults managed code cannot survive, `report` writes the report once managed code can no longer run, `logging` keeps one diagnostic file, and `ui` shows the egui window with copy, open-folder and close actions. `icon` extracts the window icon from the running executable.
- C++ shell — `shell/` owns the profiler COM object (`ICorProfilerCallback10`), takes the JIT-compilation, module-load and ReJIT event mask, and forwards every callback into the Rust core.
- Build — `build.ps1` produces `out/<rid>/nacrecraft_native.dll` (or the platform equivalent). The publish targets copy it to `runtimes/<rid>/native`.
