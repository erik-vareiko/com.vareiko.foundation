# PROJECT_CONTEXT

Последнее обновление: 2026-04-03

## 1. Цель пакета и текущий статус

Подтверждено:
- Репозиторий содержит Unity package `com.vareiko.foundation`.
- Пакет используется как инфраструктурный слой в проектах (включая `chibi_arena`).
- В версии `1.1.0` добавлены Block B инфраструктурные расширения:
  - typed cloud-command API (`ICloudCommandService`),
  - command reliability queue v2 + legacy migration,
  - deterministic RNG service (PCG32, scoped streams, snapshot/restore).

## 2. Ключевые архитектурные изменения (v1.1.0)

Подтверждено:
- Backend:
  - добавлены `CloudCommandRequest`, `CloudCommandResponse`, `BackendCommandConfig`.
  - `CloudCommandService` использует transport `ICloudFunctionService` (gateway pattern) и own reliability flow:
    - validation,
    - retryability classifier,
    - queue+persist,
    - reconnect flush,
    - TTL cleanup.
- Queue:
  - добавлены `CloudCommandQueueItem`, `ICloudCommandQueueStore`, `PlayerPrefsCloudCommandQueueStore`.
  - поддержан legacy migration из старого cloud-function queue формата (`FunctionName + PayloadJson`) в v2 записи.
- RNG:
  - добавлены `IDeterministicRngService`, `IDeterministicRngStream`, `RngStreamState`, `DeterministicRngConfig`.
  - алгоритм: фиксированный `PCG32`, scope-derivation через stable hash.
- DI/runtime:
  - `FoundationBackendInstaller` теперь биндует command stack.
  - `FoundationRuntimeInstaller` интегрирует `FoundationRngInstaller`.
  - `FoundationProjectInstaller` расширен ссылками на `BackendCommandConfig` и `DeterministicRngConfig`.

## 3. Процессные артефакты

Подтверждено:
- Обновлены:
  - `Packages/com.vareiko.foundation/README.md`
  - `Packages/com.vareiko.foundation/Documentation~/USAGE_GUIDE.md`
  - `Packages/com.vareiko.foundation/Documentation~/ARCHITECTURE.md`
  - `Packages/com.vareiko.foundation/CHANGELOG.md`
  - `Packages/com.vareiko.foundation/package.json` (version `1.1.0`)
- Добавлен feature-док:
  - `Docs/Features/BackendCommand_and_DeterministicRng_BlockB.md`.

## 4. Ограничения и компромиссы

Подтверждено:
- В MVP `IdempotencyKey` валидируется как UUIDv7 и обязателен для всех команд.
- Payload signing/HMAC не входит в текущий scope.
- Retry mapping базируется на configurable map + default heuristics; финальная server taxonomy должна быть подтверждена backend-командой.

Риски:
- Без финального server error contract возможны несовпадения retryability по edge-cases.
- При неверных server envelope shape часть кейсов может падать в fallback mapping.

## 5. Что проверять в следующих итерациях

Подтверждено:
1. Сверить фактический Azure/PlayFab error-code taxonomy с classifier rules.
2. Зафиксировать server envelope контракт для `CloudCommandResponse` fields.
3. Прогнать профилирование:
- GC на 1000 RNG вызовов,
- queue restore latency,
- queue flush allocations.
