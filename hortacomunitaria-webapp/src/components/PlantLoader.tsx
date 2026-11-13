import plantGrowing from '../assets/plant-loader.svg'
import plantStill from '../assets/plant-loader-still.svg'

type PlantLoaderProps = { className?: string }

/**
 * Indicador de carregamento da horta: uma muda que cresce em ciclo.
 * Decorativo (o texto ao lado ou o role="status" do pai anuncia o carregamento) e aparece quase de imediato
 * (80 ms), já com a muda brotada, para ser percebido mesmo em carregamentos curtos. Com movimento reduzido, mostra a muda parada.
 */
export function PlantLoader({ className = 'size-4' }: PlantLoaderProps) {
  return <picture className={`plant-loader inline-block shrink-0 ${className}`} aria-hidden="true">
    <source media="(prefers-reduced-motion: reduce)" srcSet={plantStill} />
    <img src={plantGrowing} alt="" className="block size-full" />
  </picture>
}
