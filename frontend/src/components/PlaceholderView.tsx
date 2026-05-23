type PlaceholderViewProps = {
  title: string;
  description: string;
};

export function PlaceholderView({ title, description }: PlaceholderViewProps) {
  return (
    <section>
      <div className="pageHeader">
        <div>
          <h1>{title}</h1>
          <p>{description}</p>
        </div>
      </div>

      <div className="panel">
        <p className="emptyState">This screen is scaffolded for the MVP workflow.</p>
      </div>
    </section>
  );
}
