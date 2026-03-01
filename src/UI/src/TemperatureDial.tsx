import { useRef, useState } from "react";
import cn from "./cn";

const MIN_TEMP = 30;
const MAX_TEMP = 80;

const START_ANGLE = -225;
const END_ANGLE = 45;
const ANGLE_RANGE = END_ANGLE - START_ANGLE;

const size = 320;
const cx = size / 2;
const cy = size / 2;
const radius = 140;

const circumference = 2 * Math.PI * radius;
const arcLength = circumference * (ANGLE_RANGE / 360);

type Props = {
  mainValue: number;
  knobValue: number;
  onChange: (value: number) => void;
  isLoading?: boolean;
  isHeating?: boolean;
};

export default function TemperatureDial({
  mainValue,
  knobValue,
  onChange,
  isLoading,
  isHeating,
}: Props) {
  const svgRef = useRef<SVGSVGElement>(null);
  const [dragging, setDragging] = useState(false);

  // --- VALUE → ANGLE ---
  const clamped = Math.min(Math.max(knobValue, MIN_TEMP), MAX_TEMP);
  const percent = (clamped - MIN_TEMP) / (MAX_TEMP - MIN_TEMP);
  const angle = START_ANGLE + percent * ANGLE_RANGE;
  const radians = (angle * Math.PI) / 180;

  const knobX = cx + radius * Math.cos(radians);
  const knobY = cy + radius * Math.sin(radians);

  const dashOffset = arcLength * (1 - percent);

  // --- POINTER → VALUE ---
  function updateFromPointer(clientX: number, clientY: number) {
    const rect = svgRef.current!.getBoundingClientRect();
    const x = clientX - rect.left - cx;
    const y = clientY - rect.top - cy;

    let angle = (Math.atan2(y, x) * 180) / Math.PI;

    // przesuwamy układ tak, by START_ANGLE = 0
    let relative = angle - START_ANGLE;

    // normalizacja do [0, 360)
    relative = (relative + 360) % 360;

    // obcinamy tylko do długości łuku
    const clamped = Math.min(Math.max(relative, 0), ANGLE_RANGE);

    const pct = clamped / ANGLE_RANGE;
    const newTemp = MIN_TEMP + pct * (MAX_TEMP - MIN_TEMP);

    onChange(Math.round(newTemp));
  }

  // --- EVENTS ---
  function onPointerDown(e: React.PointerEvent) {
    setDragging(true);
    svgRef.current!.setPointerCapture(e.pointerId);
    updateFromPointer(e.clientX, e.clientY);
  }

  function onPointerMove(e: React.PointerEvent) {
    if (!dragging) return;
    updateFromPointer(e.clientX, e.clientY);
  }

  function onPointerUp() {
    setDragging(false);
  }

  return (
    <div className="relative w-[320px] h-80">
      <div
        className={cn(
          "big-circle absolute inset-8 rounded-full bg-mauve-900 pointer-events-none z-0",
          isHeating ? "animate-pulse" : "opacity-0"
        )}
      />

      <svg
        ref={svgRef}
        width={size}
        height={size}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={onPointerUp}
        onPointerLeave={onPointerUp}
        className="touch-none cursor-pointer relative z-10"
      >
        {/* background arc */}
        <circle
          cx={cx}
          cy={cy}
          r={radius}
          className="stroke-gray-700 fill-transparent stroke-8"
          strokeDasharray={arcLength}
          transform={`rotate(${START_ANGLE} ${cx} ${cy})`}
        />

        {/* active arc */}
        <circle
          cx={cx}
          cy={cy}
          r={radius}
          className="stroke-cyan-700 fill-transparent stroke-8"
          strokeLinecap="round"
          strokeDasharray={arcLength}
          strokeDashoffset={dashOffset}
          transform={`rotate(${START_ANGLE} ${cx} ${cy})`}
        />

        {/* knob */}
        <g onPointerDown={onPointerDown} className="cursor-pointer">
          <circle
            cx={knobX}
            cy={knobY}
            r={16}
            className="stroke-cyan-700 fill-cyan-800 stroke-2 z-10"
          />
          {/* value inside knob */}
          <text
            x={knobX}
            y={knobY}
            dy="0.35em"
            textAnchor="middle"
            className="pointer-events-none text-xs fill-current"
          >
            {knobValue}°
          </text>
        </g>
      </svg>

      {/* center text */}
      <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
        {isLoading ? (
          <div className="text-sm text-gray-500">Loading...</div>
        ) : (
          <>
            <div className="text-6xl font-light">
              {mainValue}
              <span className="text-2xl align-top">°</span>
            </div>
            <div className="uppercase text-xs tracking-widest text-gray-400">
              Water temperature
            </div>
          </>
        )}
      </div>
    </div>
  );
}
