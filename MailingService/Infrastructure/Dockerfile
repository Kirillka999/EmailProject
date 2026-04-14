FROM rabbitmq:4.0-management

ENV DELAYED_MESSAGE_PLUGIN_VERSION=4.0.7

ADD --chown=rabbitmq:rabbitmq \
    https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${DELAYED_MESSAGE_PLUGIN_VERSION}/rabbitmq_delayed_message_exchange-v${DELAYED_MESSAGE_PLUGIN_VERSION}.ez \
    /plugins/

RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange