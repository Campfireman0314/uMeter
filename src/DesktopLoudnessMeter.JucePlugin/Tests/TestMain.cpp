#include <exception>
#include <functional>
#include <iostream>
#include <string>
#include <vector>

void runLoopbackProtocolTests();
void runLoopbackSenderTests();
void runPluginStateTests();
void runRealtimeAudioQueueTests();

namespace
{
struct TestCase
{
    const char* name;
    std::function<void()> run;
};
}

int main()
{
    const std::vector<TestCase> tests
    {
        { "LoopbackProtocol", runLoopbackProtocolTests },
        { "LoopbackSender", runLoopbackSenderTests },
        { "PluginState", runPluginStateTests },
        { "RealtimeAudioQueue", runRealtimeAudioQueueTests }
    };

    auto failures = 0;

    for (const auto& test : tests)
    {
        try
        {
            test.run();
            std::cout << "[pass] " << test.name << '\n';
        }
        catch (const std::exception& ex)
        {
            ++failures;
            std::cerr << "[fail] " << test.name << ": " << ex.what() << '\n';
        }
    }

    return failures == 0 ? 0 : 1;
}
