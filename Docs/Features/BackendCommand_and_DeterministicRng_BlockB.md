# BackendCommand + DeterministicRng (Block B)

Дата: 2026-04-03
Версия пакета: 1.1.0

## 1. Назначение

Расширение `com.vareiko.foundation` для Block B:
- typed idempotent backend-команды поверх cloud function transport;
- retry/queue/persistence для command flow;
- deterministic RNG инфраструктура для seed/scoped run-логики.

## 2. Архитектурная роль

### Backend command layer
- Вход: `CloudCommandRequest`.
- Выход: `CloudCommandResponse`.
- Сервис: `ICloudCommandService` (`CloudCommandService`).
- Transport: `ICloudFunctionService` через gateway function (`BackendCommandConfig.GatewayFunctionName`, default `CommandGateway`).
- Ответственность слоя:
  - валидация запроса;
  - retryability classification;
  - queueing retryable failures;
  - restore/flush pending queue;
  - TTL cleanup queued entries.

### Queue v2 layer
- Контракт: `ICloudCommandQueueStore`.
- Реализация: `PlayerPrefsCloudCommandQueueStore`.
- Модель: `CloudCommandQueueItem`.
- Поля v2:
  - function/payload,
  - serialized request json,
  - idempotency key,
  - attempt count,
  - first queued unix ms,
  - last attempt unix ms.
- Миграция:
  - при отсутствии v2 данных читается legacy очередь (`FunctionName + PayloadJson`);
  - валидные элементы конвертируются в v2;
  - невалидные элементы пропускаются с warning;
  - restore flow не падает на mixed/invalid данных.

### Deterministic RNG layer
- Контракт сервиса: `IDeterministicRngService`.
- Контракт потока: `IDeterministicRngStream`.
- Состояние: `RngStreamState`.
- Алгоритм: `PCG32` (явно фиксирован в коде).
- Поведение:
  - `Initialize(rootSeed)` задает root seed;
  - `CreateStream(scope)` возвращает scoped deterministic stream;
  - `RestoreStream(scope, state)` восстанавливает состояние потока.

## 3. Data Flow

### Command flow
1. Gameplay формирует `CloudCommandRequest`.
2. `CloudCommandService` валидирует request.
3. Request сериализуется в transport envelope.
4. Команда отправляется через `ICloudFunctionService`.
5. Ответ маппится в `CloudCommandResponse`.
6. Retryable failures -> queue; non-retryable -> terminal fail; duplicate/idempotency conflict -> success-like terminal.
7. На reconnect выполняется queue flush.

### RNG flow
1. Run startup вызывает `Initialize(rootSeed)`.
2. Системы создают streams по scope (`run.nodes`, `run.choices`, `battle.proc`).
3. При save/load используется `CaptureState`/`RestoreStream`.

## 4. Публичные контракты и конфигурация

### New public APIs
- `ICloudCommandService`
- `CloudCommandRequest`
- `CloudCommandResponse`
- `IDeterministicRngService`
- `IDeterministicRngStream`
- `RngStreamState`

### New configs
- `BackendCommandConfig`
  - `GatewayFunctionName` (default `CommandGateway`)
  - `DefaultRequestVersion` (default `1`)
  - `MaxPayloadBytes` (default `65536`)
  - `QueueTtlHours` (default `24`)
  - `ErrorRules` (errorCode->retryability/success-like)
- `DeterministicRngConfig`
  - `DefaultRootSeed`
  - `AllowReseedAtRuntime`
  - `EnableRngDiagnostics`

## 5. Ограничения

- `IdempotencyKey` обязателен и валидируется как UUIDv7.
- Payload signing/HMAC отсутствует в v1.1.0.
- Финальный retry mapping зависит от server taxonomy; текущая карта частично эвристическая.

## 6. Manual QA Checklist

1. Offline enqueue -> reconnect flush.
2. Duplicate command with same idempotency key -> no duplicated effect.
3. Non-retryable errors stop retries.
4. Legacy queue data migrates and flushes without crash.
5. Same seed/scope/call sequence returns same RNG sequence.
6. `CaptureState/RestoreStream` reproduces next values.

## 7. Performance Evaluation

- Context:
  - command path event-driven;
  - RNG potentially hot path.
- CPU/GC:
  - expected no GC in `Next*` and `PickWeightedIndex` normal path;
  - queue flush allocations linear by pending item count.
- Memory:
  - queue bounded by `MaxQueuedCloudFunctions` + TTL;
  - scoped stream registry grows by active scopes.
- Mandatory profiling:
  - GC on 1000 RNG calls,
  - startup queue restore latency,
  - queue flush latency/allocations.
