export function ModeButton({
  label,
  active,
  onClick,
}: {
  label: string;
  active: boolean;
  onClick?: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={`w-20 h-20 rounded-full flex items-center justify-center
        ${active ? "bg-danger" : "bg-white/10"}
      `}
    >
      <span className="text-sm">{label}</span>
    </button>
  );
}
