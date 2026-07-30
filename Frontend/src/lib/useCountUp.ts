import { useEffect, useRef, useState } from "react";
import { animate } from "framer-motion";

// Плавный счётчик: от 0 при самой первой отрисовке (страница только что
// загрузилась — это и есть момент, когда анимация должна быть видна), а при
// последующих обновлениях данных — от предыдущего значения к новому, а не
// снова от нуля.
export function useCountUp(value: number, durationSeconds = 0.8) {
  const [display, setDisplay] = useState(0);
  const prevValue = useRef(0);

  useEffect(() => {
    const controls = animate(prevValue.current, value, {
      duration: durationSeconds,
      ease: "easeOut",
      onUpdate: setDisplay,
    });
    prevValue.current = value;
    return () => controls.stop();
  }, [value, durationSeconds]);

  return display;
}
