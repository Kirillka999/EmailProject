# 🚀 ULTRA-MAILER V2: Fault-Tolerant Microservices Grid 🚀

## 💡 Философия проекта
Забудь про блокирующие вызовы `SmtpClient` в основном API. Это **Production-Ready Event-Driven Ecosystem**, построенная на принципах полного **Decoupling** и строгой сетевой изоляции. Мы не просто шлем письма — мы оперируем **Message Streams** в распределенной среде, где контейнеры смертны, а данные — вечны. 💎

## 🏗️ Архитектурный Stack & Security
Проект задизайнен по хардкорным канонам **Microservices Best Practices** с независимыми пайплайнами развертывания (у каждого слоя свой `docker-compose.yml`):

*   🛡️ **Email API Gateway (Producer):** Наш ингресс-поинт. Сидит в публичной сети, принимает хай-лоад трафик, валидирует Payload и мгновенно диспатчит его в месседж-брокер. Отвечает `200 OK` за наносекунды.
*   🐇 **RabbitMQ (Message Broker):** Наша транзакционная шина данных. Теперь с **Persistent Volumes** — выдержит любой рестарт сервера без потери сообщений в очередях.
*   📧 **Mailing Worker (Consumer):** Автономный "тяжеловес". Слушает очередь, рендерит **Razor-шаблоны** на лету и отправляет письма. Сидит на двух стульях: общается с брокером через общую сеть и имеет эксклюзивный доступ к БД через изолированную подсеть.
*   🗄️ **PostgreSQL (State):** Хранит логи отправки. Полностью спрятана от внешнего мира и API (паттерн *Database-per-Service*). Данные пишутся на диск хоста через **Volumes**.

## 🔥 Почему это Top-Tier Architecture?
*   **Circuit Breaker & KillSwitch:** Лимиты Google SMTP нас не пугают. Если Google отдаст `429 Too Many Requests`, консьюмер поймает эксепшн, "выбьет пробки" (KillSwitch) и поставит обработку на паузу, спасая репутацию IP-адреса.
*   **Auto-Recovery (Reprocessor):** Упавшие письма не исчезают. Фоновый воркер `ErrorQueueReprocessorService` сам перенесет их из `_error` очереди обратно в основную, когда таймаут наказания пройдет.
*   **Horizontal Scalability:** Нужно слать в 10 раз больше? Просто подними еще 10 инстансов Консьюмера, RabbitMQ сам отбалансирует нагрузку по Round-Robin.
*   **Strict Isolation:** Ни один хакер, взломавший API, не сможет дотянуться до базы данных почтовика. Они физически находятся в разных Docker-сетях. 🔐

---

## 🛠️ Развертывание кластера (Как запустить)

Поскольку это настоящие микросервисы, мы поднимаем их послойно. Все команды выполняются из **корня проекта**.

**Шаг 1: Подъем инфраструктуры (RabbitMQ)**
```bash
docker compose -f Infrastructure/docker-compose.yml up -d
```

**Шаг 2: Подъем микросервиса рассылки 🤗** (Здесь Докер сам соберет C# код)
```bash
docker compose -f MailingService/docker-compose.yml up -d --build
```

**Шаг 3: Подъем API Gateway**
```bash
docker compose -f EmailProject/docker-compose.yml up -d --build
```

*(Чтобы быстро погасить весь кластер: сделай `docker compose down` для каждого файла. БД и очереди при этом сохраняются)*

---

## 📨 Payload Example (Event Contract)
Дергай наш эндпоинт как настоящий Software Architect. Мы передаем не текст, а строго типизированные события с JSON-пейлоадом, которые резолвятся через рефлексию и RazorLight:

```json
{
  "email": "ceo@google.com",
  "subject": "Доступ к системе выдан",
  "templateName": "WelcomeTemplate.cshtml",
  "modelTypeName": "Shared.Templates.WelcomeTemplate, Shared",
  "payload": "{\"UserName\":\"Sundar\",\"Test\":\"Welcome to the Matrix\"}"
}
```

---
**Powered by MassTransit, Docker Compose, RabbitMQ and absolute Over-Engineering.** ✨💅