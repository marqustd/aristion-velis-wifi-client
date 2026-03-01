import { ModeButton } from "./ModeButton";
import TemperatureDial from "./TemperatureDial";
import { useFetchSensors } from "./api";

const refetchInterval = 30 * 1000;

export default function App() {
  const { data: sensors, isLoading } = useFetchSensors({
    query: { refetchInterval },
  });

  return (
    <div className="min-h-screen flex flex-col items-center justify-between py-10">
      {/* Status */}
      <div className="flex items-center gap-10 text-sm text-muted">
        <div className="text-center">
          <div className="uppercase">Available showers</div>
          <div className="text-xl text-white font-semibold">
            {sensors?.data.availableShowers ?? 0}/4
          </div>
        </div>

        <div className="w-px h-10 bg-muted/30" />

        <div className="text-center">
          <div className="uppercase">At temperature</div>
          <div className="text-white">{sensors?.data.remainingTime}</div>
        </div>
      </div>

      {/* Dial */}
      <TemperatureDial
        mainValue={sensors?.data.currentTemperature ?? 69}
        knobValue={sensors?.data.requiredTemperature ?? 47}
        onChange={() => null}
        isLoading={isLoading}
        isHeating={sensors?.data.isHeating ?? false}
      />

      {/* Modes */}
      <div className="flex gap-10 pb-4">
        <ModeButton label="Manual" active={sensors?.data.mode === "Manual"} />
        <ModeButton
          label="Scheduled"
          active={sensors?.data.mode === "Scheduled"}
        />
        <ModeButton label="Eco" active={sensors?.data.mode === "Eco"} />
      </div>
    </div>
  );
}
