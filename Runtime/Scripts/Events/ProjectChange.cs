using UniRx;
using System;
using Project;

namespace Virgis {

    public class ProjectChange {

        private GisProject _project;

        private readonly Subject<GisProject> _projectEvent = new Subject<GisProject>();

        public void Set(GisProject project) {
            _project = project;
        }

        public void Complete() {
            _projectEvent.OnNext(_project);
        }

        public GisProject Get() {
            return _project;
        }

        public IObservable<GisProject> Event {
            get {
                return _projectEvent.AsObservable();
            }
        }

    }

}
